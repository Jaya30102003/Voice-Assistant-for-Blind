// Models/ViewModels/EducationItemViewModel.cs
namespace VoiceAssistantForBlind.Models.ViewModels
{
    public class EducationItemViewModel
    {
        public int Id { get; set; }
        public string? Degree { get; set; }
        public string? Institution { get; set; }
        public string? Duration { get; set; }
        public List<string> Highlights { get; set; } = new();
    }
}