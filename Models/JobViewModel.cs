using System.ComponentModel.DataAnnotations;

namespace VoiceAssistantForBlind.Models
{
    public class JobViewModel
    {
        public int Id { get; set; }
        
        // Make JobCode not required for creation since it's auto-generated
        [Display(Name = "Job Code")]
        [StringLength(20)]
        public string? JobCode { get; set; } // Removed [Required] attribute
        
        [Required(ErrorMessage = "Company Name is required")]
        [Display(Name = "Company Name")]
        [StringLength(100)]
        public string? CompanyName { get; set; }
        
        [Required(ErrorMessage = "Job Title is required")]
        [Display(Name = "Job Title")]
        [StringLength(100)]
        public string? JobTitle { get; set; }
        
        [Required(ErrorMessage = "Required Skills are required")]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }
        
        [Display(Name = "Skills List")]
        public List<string> SkillsList { get; set; } = new();
        
        [Display(Name = "Job Description")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Last Date to Apply is required")]
        [Display(Name = "Last Date to Apply")]
        [DataType(DataType.Date)]
        public DateTime LastDateToApply { get; set; } = DateTime.Now.AddDays(30);
        
        [Required(ErrorMessage = "HR Email is required")]
        [EmailAddress]
        [Display(Name = "HR Email")]
        public string? HREmail { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}