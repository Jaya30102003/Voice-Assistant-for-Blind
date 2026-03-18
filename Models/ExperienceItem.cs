// Models/ExperienceItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceAssistantForBlind.Models
{
    public class ExperienceItem
    {
        [Key]
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Location { get; set; }
        public string? Duration { get; set; }
        public string? Highlights { get; set; }
        
        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}