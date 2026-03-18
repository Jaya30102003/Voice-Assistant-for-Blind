// Models/EmailSettings.cs
namespace VoiceAssistantForBlind.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; }
        public string SenderName { get; set; } = "Voice Assistant for Blind";
        public string AppPassword { get; set; }
        public bool EnableSsl { get; set; } = true;
    }
}