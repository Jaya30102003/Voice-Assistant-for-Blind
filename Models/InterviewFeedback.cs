namespace VoiceAssistantForBlind.Models
{
    public class InterviewFeedback
    {
        public int Score { get; set; } // 1-10
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public string? SampleBetterAnswer { get; set; }
        public string? Summary { get; set; }
        public string? KeyConcepts { get; set; }
        public string? MissingConcepts { get; set; }
    }
}