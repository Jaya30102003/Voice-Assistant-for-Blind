// Models/ViewModels/ProjectItemViewModel.cs
namespace VoiceAssistantForBlind.Models.ViewModels
{
    public class ProjectItemViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public List<string> Highlights { get; set; } = new();
    }
}