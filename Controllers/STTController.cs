using Microsoft.AspNetCore.Mvc;
using NerApi.Services;
using NerApi.Models;

namespace NerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SttController : ControllerBase
    {
        private readonly IWhisperTranscriptionService _stt;
        private readonly INerService _ner;

        public SttController(IWhisperTranscriptionService stt, INerService ner)
        {
            _stt = stt;
            _ner = ner;
        }

        // POST /api/stt/transcribe  (multipart/form-data, "audio" = WAV 16k mono PCM16)
        [HttpPost("transcribe")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Transcribe([FromForm] IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest("No audio uploaded.");

            using var ms = new MemoryStream();
            await audio.CopyToAsync(ms);

            var text = await _stt.TranscribeAsync(ms);
            var entities = string.IsNullOrWhiteSpace(text)
                ? new List<NerEntity>()
                : _ner.ExtractEntities(text);

            return Ok(new { text, entities }); // PascalCase enforced by Program.cs
        }
    }
}