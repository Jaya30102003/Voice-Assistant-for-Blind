// Models/ViewModels/ExperienceItemViewModel.cs
namespace VoiceAssistantForBlind.Models.ViewModels
{
    public class ExperienceItemViewModel
    {
        public int Id { get; set; }
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Location { get; set; }
        public string? Duration { get; set; }
        public List<string> Highlights { get; set; } = new();
    }
}