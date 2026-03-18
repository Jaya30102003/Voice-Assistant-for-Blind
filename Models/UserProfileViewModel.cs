// Models/UserProfileViewModel.cs
using System.ComponentModel.DataAnnotations;
using VoiceAssistantForBlind.Models.ViewModels;

namespace VoiceAssistantForBlind.Models
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }
        
        [Url(ErrorMessage = "Invalid URL")]
        [Display(Name = "LinkedIn Profile URL")]
        public string? LinkedIn { get; set; }
        
        [Url(ErrorMessage = "Invalid URL")]
        [Display(Name = "GitHub Profile URL")]
        public string? GitHub { get; set; }
        
        [Display(Name = "Languages (e.g., English, Tamil, Hindi)")]
        public string? Languages { get; set; }
        
        [Display(Name = "Technical Concepts (e.g., OOP, REST APIs, DBMS)")]
        public string? Concepts { get; set; }
        
        [Display(Name = "Software/Tools (e.g., VS Code, Git, Docker)")]
        public string? Software { get; set; }
        
        // Use ViewModel classes for the form
        public List<EducationItemViewModel> Education { get; set; } = new();
        public List<ExperienceItemViewModel> Experience { get; set; } = new();
        public List<ProjectItemViewModel> Projects { get; set; } = new();
        public List<string> Certifications { get; set; } = new();
        public List<string> Achievements { get; set; } = new();
    }
}