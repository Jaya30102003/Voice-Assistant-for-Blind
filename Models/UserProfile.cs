// Models/UserProfile.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceAssistantForBlind.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        
        [Phone]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }
        
        [Url]
        [Display(Name = "LinkedIn Profile")]
        public string? LinkedIn { get; set; }
        
        [Url]
        [Display(Name = "GitHub Profile")]
        public string? GitHub { get; set; }
        
        [Display(Name = "Languages (comma separated)")]
        public string? Languages { get; set; }
        
        [Display(Name = "Technical Concepts")]
        public string? Concepts { get; set; }
        
        [Display(Name = "Software/Tools")]
        public string? Software { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties for resume sections
        public virtual ICollection<EducationItem>? Education { get; set; }
        public virtual ICollection<ExperienceItem>? Experience { get; set; }
        public virtual ICollection<ProjectItem>? Projects { get; set; }
        public virtual ICollection<Certification>? Certifications { get; set; }
        public virtual ICollection<Achievement>? Achievements { get; set; }
    }
    
    public class Certification
    {
        [Key]
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string? Name { get; set; }
        
        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
    
    public class Achievement
    {
        [Key]
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string? Description { get; set; }
        
        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}