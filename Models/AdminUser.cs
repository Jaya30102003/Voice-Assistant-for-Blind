using System.ComponentModel.DataAnnotations;

namespace VoiceAssistantForBlind.Models
{
    public class AdminUser
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
        public string? PasswordHash { get; set; } // Store hashed password
        
        [Required]
        public string? Salt { get; set; } // For password hashing
        
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        [Display(Name = "Role")]
        public string? Role { get; set; } = "Admin";
        
        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
    }
}