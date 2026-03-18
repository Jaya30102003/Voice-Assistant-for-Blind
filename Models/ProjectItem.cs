// Models/ProjectItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceAssistantForBlind.Models
{
    public class ProjectItem
    {
        [Key]
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public string? Highlights { get; set; }
        
        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}