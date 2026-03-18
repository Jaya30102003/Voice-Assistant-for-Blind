using System.ComponentModel.DataAnnotations;

namespace VoiceAssistantForBlind.Models
{
    public class InterviewQuestion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Topic { get; set; } = string.Empty; // "C#", "Python", "SQL"
        
        [Required]
        public string Question { get; set; } = string.Empty;
        
        public string? ExpectedKeywords { get; set; } // Optional: for basic validation
        
        public string? SampleAnswer { get; set; } // Optional: reference for LLM
        
        public string Difficulty { get; set; } = "Intermediate"; // Beginner/Intermediate/Advanced
        
        public int DisplayOrder { get; set; }
    }
}