// Services/IEmailService.cs
using System.Threading.Tasks;
using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Services
{
    public interface IEmailService
    {
        Task<EmailSendResult> SendJobApplication(string hrEmail, string jobTitle, string company, string jobCode);
    }

    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorDetails { get; set; }
        public bool RequiresProfile { get; set; } 
    }
}
