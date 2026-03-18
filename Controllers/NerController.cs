using Microsoft.AspNetCore.Mvc;
using NerApi.Models;
using NerApi.Services;

namespace NerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NerController : ControllerBase
    {
        private readonly INerService _ner;

        public NerController(INerService nerService)
        {
            _ner = nerService;
        }

        [HttpPost("extract")]
        public IActionResult Extract([FromBody] NerRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text cannot be empty.");

            var entities = _ner.ExtractEntities(req.Text);
            return Ok(entities);
        }
    }
}