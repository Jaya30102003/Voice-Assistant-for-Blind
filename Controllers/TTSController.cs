using Microsoft.AspNetCore.Mvc;
using NerApi.Models;
using System.Speech.Synthesis;

namespace NerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextController : ControllerBase
    {
        [HttpPost("speak")]
        public IActionResult Speak([FromBody] TtsRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Text))
                return BadRequest("Text cannot be empty.");

            using var synth = new SpeechSynthesizer();
            synth.SetOutputToWaveStream(new MemoryStream());
            using var ms = new MemoryStream();
            synth.SetOutputToWaveStream(ms);
            synth.Speak(req.Text);
            ms.Position = 0;
            return File(ms.ToArray(), "audio/wav", "tts.wav");
        }
    }
}