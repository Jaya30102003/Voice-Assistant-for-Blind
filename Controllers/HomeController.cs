using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VoiceAssistantForBlind.Controllers
{
    [Authorize] // This requires authentication for ALL actions in this controller
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}