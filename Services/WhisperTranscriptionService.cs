using System.Text;
using Whisper.net;

namespace NerApi.Services
{
    public sealed class WhisperTranscriptionService : IWhisperTranscriptionService
    {
        private readonly WhisperFactory _factory;
        private readonly string _language;

        public WhisperTranscriptionService(string modelPath, string language)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Whisper GGML model not found: {modelPath}");

            _factory  = WhisperFactory.FromPath(modelPath);
            _language = string.IsNullOrWhiteSpace(language) ? "en" : language;
        }

        public async Task<string> TranscribeAsync(Stream audioStream)
        {
            audioStream.Position = 0;

            using var processor = _factory.CreateBuilder()
                                          .WithLanguage(_language) // or "auto"
                                          .Build();

            var sb = new StringBuilder();
            await foreach (var seg in processor.ProcessAsync(audioStream))
            {
                sb.Append(seg.Text);
                if (!seg.Text.EndsWith(" ")) sb.Append(' ');
            }
            return sb.ToString().Trim();
        }
    }
}