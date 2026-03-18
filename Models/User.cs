using System.ComponentModel.DataAnnotations;

namespace VoiceAssistantForBlind.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string? Username { get; set; }
        
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        
        [Required]
        public string? PasswordHash { get; set; }
        
        [Required]
        public string? Salt { get; set; }
        
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        // Link to existing UserProfile (one-to-one relationship)
        public int? UserProfileId { get; set; }
        public virtual UserProfile? UserProfile { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}