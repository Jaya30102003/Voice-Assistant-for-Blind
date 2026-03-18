using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceAssistantForBlind.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Job Code")]
        [StringLength(20)]
        public string? JobCode { get; set; } // e.g., ABC001, TECH002
        
        [Required]
        [Display(Name = "Company Name")]
        [StringLength(100)]
        public string? CompanyName { get; set; }
        
        [Required]
        [Display(Name = "Job Title")]
        [StringLength(100)]
        public string? JobTitle { get; set; }
        
        [Required]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; } // Store as comma-separated: "C, C++, Java, Python"
        
        [Display(Name = "Job Description")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "Last Date to Apply")]
        [DataType(DataType.Date)]
        public DateTime LastDateToApply { get; set; }
        
        [Required]
        [EmailAddress]
        [Display(Name = "HR Email")]
        public string? HREmail { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }
    }
}