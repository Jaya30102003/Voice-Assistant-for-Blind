// Models/ResumeRequest.cs
using System.Collections.Generic;

namespace VoiceAssistantForBlind.Models
{
    public class ResumeRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LinkedIn { get; set; }
        public string? GitHub { get; set; }

        public string? Languages { get; set; }
        public string? Concepts { get; set; }
        public string? Software { get; set; }

        // Use the shared model classes
        public List<EducationItem>? Education { get; set; }
        public List<ExperienceItem>? Experience { get; set; }
        public List<ProjectItem>? Projects { get; set; }

        public List<string>? Certifications { get; set; }
        public List<string>? Achievements { get; set; }
    }
}