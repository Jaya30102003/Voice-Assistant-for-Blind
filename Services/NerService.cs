using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers; // managed, cross-platform
using NerApi.Models;

namespace NerApi.Services
{
    public class NerService : INerService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Tokenizer _tokenizer; // WordPieceTokenizer base type
        private readonly string[] _id2label;
        private readonly string[] _id2token;
        private readonly string _subPrefix = "##"; // safe default
        private readonly int? _clsId;
        private readonly int? _sepId;

        public NerService(IHostEnvironment env)
        {
            var root = env.ContentRootPath;

            var modelPath     = Path.Combine(root, "NERModel", "model.onnx");
            var tokenizerPath = Path.Combine(root, "NERModel", "tokenizer.json");

            // ONNX session (cross-platform)
            _session = new InferenceSession(modelPath);

            // Build WordPiece tokenizer from tokenizer.json
            (_id2token, _subPrefix, _clsId, _sepId) = LoadWordPieceFromHfTokenizerJson(tokenizerPath);

            // Create a vocab.txt (one token per line in id order) in-memory
            using var vocabStream = BuildVocabStream(_id2token);

            // Create managed WordPiece tokenizer (no native deps)
            var options = new WordPieceOptions
            {
                UnknownToken = "[UNK]",
                ContinuingSubwordPrefix = _subPrefix ?? "##",
                MaxInputCharsPerWord = 200
            };
            _tokenizer = WordPieceTokenizer.Create(vocabStream, options);

            _id2label = new[]
            {
                "O", "B-MISC", "I-MISC",
                "B-PER", "I-PER",
                "B-ORG", "I-ORG",
                "B-LOC", "I-LOC"
            };
        }

        public List<NerEntity> ExtractEntities(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<NerEntity>();

            // Encode text to token IDs
            var idsList = _tokenizer.EncodeToIds(text);

            // Optionally add special tokens if vocabulary has them
            var ids = new List<int>(idsList.Count + 2);
            if (_clsId.HasValue) ids.Add(_clsId.Value);
            ids.AddRange(idsList);
            if (_sepId.HasValue) ids.Add(_sepId.Value);

            int seqLen = ids.Count;

            var inputIds      = new DenseTensor<long>(new[] { 1, seqLen });
            var attentionMask = new DenseTensor<long>(new[] { 1, seqLen });
            var typeIds       = new DenseTensor<long>(new[] { 1, seqLen });

            for (int i = 0; i < seqLen; i++)
            {
                inputIds[0, i]      = ids[i];
                attentionMask[0, i] = 1;
                typeIds[0, i]       = 0;
            }

            var inputs = new[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids",      inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeIds),
            };

            using var results = _session.Run(inputs);
            var logits = results.First().AsTensor<float>();

            int numLabels = logits.Dimensions[2];
            var pred = new int[seqLen];

            for (int i = 0; i < seqLen; i++)
            {
                float max = float.NegativeInfinity;
                int maxIdx = 0;

                for (int l = 0; l < numLabels; l++)
                {
                    float v = logits[0, i, l];
                    if (v > max) { max = v; maxIdx = l; }
                }
                pred[i] = (maxIdx >= 0 && maxIdx < _id2label.Length) ? maxIdx : 0; // default "O"
            }

            // Entity reconstruction
            var entities = new List<NerEntity>();
            int start = -1, end = -1;
            string? type = null;

            bool IsContinuation(int idx)
            {
                if (idx < 0 || idx >= ids.Count) return false;
                int tid = ids[idx];
                if (tid < 0 || tid >= _id2token.Length) return false;
                var tok = _id2token[tid];
                return !string.IsNullOrEmpty(tok) && !string.IsNullOrEmpty(_subPrefix) && tok.StartsWith(_subPrefix);
            }

            void Flush()
            {
                if (type == null || start < 0 || end < start) return;

                int extEnd = end;
                while (extEnd + 1 < seqLen && IsContinuation(extEnd + 1))
                    extEnd++;

                if (IsValidSpan(start, extEnd, ids.Count))
                {
                    var surface = Reconstruct(ids.Select(x => (uint)x).ToArray(), start, extEnd);

                    if (type == "PER" && surface.Length <= 1)
                        return;

                    if (!string.IsNullOrWhiteSpace(surface))
                    {
                        entities.Add(new NerEntity
                        {
                            Entity = surface,
                            Type   = type
                        });
                    }
                }
            }

            for (int i = 0; i < seqLen; i++)
            {
                string tag = _id2label[pred[i]];

                if (tag.StartsWith("B-"))
                {
                    Flush();
                    type  = tag.Substring(2);
                    start = end = i;
                }
                else if (tag.StartsWith("I-") && type == tag.Substring(2))
                {
                    end = i;
                }
                else
                {
                    Flush();
                    type  = null;
                    start = end = -1;
                }
            }

            Flush();
            return entities;
        }

        private static bool IsValidSpan(int start, int end, int length)
            => start >= 0 && end >= start && end < length;

        private (string[] id2token, string prefix, int? clsId, int? sepId)
            LoadWordPieceFromHfTokenizerJson(string tokenizerJson)
        {
            using var fs  = File.OpenRead(tokenizerJson);
            using var doc = JsonDocument.Parse(fs);

            var root = doc.RootElement;
            var model = root.GetProperty("model");

            string prefix = model.TryGetProperty("continuing_subword_prefix", out var p)
                ? p.GetString() ?? "##"
                : "##";

            var vocab = model.GetProperty("vocab");
            int maxId = vocab.EnumerateObject().Max(e => e.Value.GetInt32());
            var id2token = new string[maxId + 1];

            foreach (var kv in vocab.EnumerateObject())
                id2token[kv.Value.GetInt32()] = kv.Name;

            // special tokens (if present)
            int? clsId = TryGetTokenId(vocab, "[CLS]");
            int? sepId = TryGetTokenId(vocab, "[SEP]");

            return (id2token, prefix, clsId, sepId);

            static int? TryGetTokenId(JsonElement vocabObj, string tok)
            {
                if (vocabObj.TryGetProperty(tok, out var val))
                    return val.GetInt32();
                return null;
            }
        }

        private static MemoryStream BuildVocabStream(string[] id2token)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < id2token.Length; i++)
                sb.AppendLine(id2token[i] ?? string.Empty);
            return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private string Reconstruct(uint[] ids, int start, int end)
        {
            if (ids == null || ids.Length == 0) return string.Empty;
            start = Math.Max(0, start);
            end   = Math.Min(end, ids.Length - 1);
            if (start > end) return string.Empty;

            var words = new List<string>();

            for (int i = start; i <= end; i++)
            {
                int tid = (int)ids[i];
                if (tid < 0 || tid >= _id2token.Length) continue;

                string? tok = _id2token[tid];

                if (tok is null or "[CLS]" or "[SEP]" or "[PAD]" or "[UNK]")
                    continue;

                if (!string.IsNullOrEmpty(_subPrefix) && tok.StartsWith(_subPrefix))
                {
                    string piece = tok.Substring(_subPrefix.Length);
                    if (words.Count > 0)
                        words[^1] += piece;
                    else
                        words.Add(piece);
                }
                else
                {
                    words.Add(tok);
                }
            }
            return string.Join(" ", words).Trim();
        }

        public void Dispose() => _session?.Dispose();
    }
}