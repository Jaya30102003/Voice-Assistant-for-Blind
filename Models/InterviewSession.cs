namespace VoiceAssistantForBlind.Models
{
    public class InterviewSession
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalQuestions { get; set; }
        public int QuestionsAnswered { get; set; }
        public double AverageScore { get; set; }
        public List<InterviewAnswer> Answers { get; set; } = new();
    }

    public class InterviewAnswer
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public int Score { get; set; }
        public string? Feedback { get; set; }
        public string? Strengths { get; set; }
        public string? Improvements { get; set; }
        public DateTime AnsweredAt { get; set; }
    }
}