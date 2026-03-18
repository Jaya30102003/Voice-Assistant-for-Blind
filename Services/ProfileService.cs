using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Models.ViewModels;

namespace VoiceAssistantForBlind.Services
{
    public class ProfileService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProfileService> _logger;
        
        public ProfileService(AppDbContext context, ILogger<ProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<UserProfileViewModel?> GetLatestProfileAsync()
        {
            var profile = await _context.UserProfiles
                .Include(u => u.Education)
                .Include(u => u.Experience)
                .Include(u => u.Projects)
                .Include(u => u.Certifications)
                .Include(u => u.Achievements)
                .OrderByDescending(u => u.UpdatedAt ?? u.CreatedAt)
                .FirstOrDefaultAsync();
                
            return profile == null ? null : MapToViewModel(profile);
        }
        
        // NEW: Get profile by ID
        public async Task<UserProfileViewModel?> GetProfileByIdAsync(int id)
        {
            var profile = await _context.UserProfiles
                .Include(u => u.Education)
                .Include(u => u.Experience)
                .Include(u => u.Projects)
                .Include(u => u.Certifications)
                .Include(u => u.Achievements)
                .FirstOrDefaultAsync(u => u.Id == id);
                
            return profile == null ? null : MapToViewModel(profile);
        }
        
        public async Task<UserProfileViewModel> SaveProfileAsync(UserProfileViewModel viewModel)
        {
            Console.WriteLine("========== SaveProfileAsync ==========");
            Console.WriteLine($"Saving profile for: {viewModel.FullName}");
            
            UserProfile profile;
            
            if (viewModel.Id > 0)
            {
                profile = await _context.UserProfiles
                    .Include(u => u.Education)
                    .Include(u => u.Experience)
                    .Include(u => u.Projects)
                    .Include(u => u.Certifications)
                    .Include(u => u.Achievements)
                    .FirstOrDefaultAsync(u => u.Id == viewModel.Id);
                    
                if (profile == null) throw new Exception("Profile not found");
                
                // Clear existing collections
                profile.Education?.Clear();
                profile.Experience?.Clear();
                profile.Projects?.Clear();
                profile.Certifications?.Clear();
                profile.Achievements?.Clear();
            }
            else
            {
                profile = new UserProfile();
                await _context.UserProfiles.AddAsync(profile);
            }
            
            // Update basic info
            profile.FullName = viewModel.FullName;
            profile.Email = viewModel.Email;
            profile.Phone = viewModel.Phone;
            profile.LinkedIn = viewModel.LinkedIn;
            profile.GitHub = viewModel.GitHub;
            profile.Languages = viewModel.Languages;
            profile.Concepts = viewModel.Concepts;
            profile.Software = viewModel.Software;
            profile.UpdatedAt = DateTime.UtcNow;
            
            // Save Education items
            if (viewModel.Education != null && viewModel.Education.Any())
            {
                profile.Education = new List<EducationItem>();
                foreach (var edu in viewModel.Education)
                {
                    profile.Education.Add(new EducationItem
                    {
                        Degree = edu.Degree,
                        Institution = edu.Institution,
                        Duration = edu.Duration,
                        Highlights = edu.Highlights != null && edu.Highlights.Any() 
                            ? JsonConvert.SerializeObject(edu.Highlights) 
                            : null
                    });
                }
            }
            
            // Save Experience items
            if (viewModel.Experience != null && viewModel.Experience.Any())
            {
                profile.Experience = new List<ExperienceItem>();
                foreach (var exp in viewModel.Experience)
                {
                    profile.Experience.Add(new ExperienceItem
                    {
                        Company = exp.Company,
                        Role = exp.Role,
                        Location = exp.Location,
                        Duration = exp.Duration,
                        Highlights = exp.Highlights != null && exp.Highlights.Any() 
                            ? JsonConvert.SerializeObject(exp.Highlights) 
                            : null
                    });
                }
            }
            
            // Save Projects items
            if (viewModel.Projects != null && viewModel.Projects.Any())
            {
                profile.Projects = new List<ProjectItem>();
                foreach (var proj in viewModel.Projects)
                {
                    profile.Projects.Add(new ProjectItem
                    {
                        Title = proj.Title,
                        Duration = proj.Duration,
                        Highlights = proj.Highlights != null && proj.Highlights.Any() 
                            ? JsonConvert.SerializeObject(proj.Highlights) 
                            : null
                    });
                }
            }
            
            // Save Certifications
            if (viewModel.Certifications != null && viewModel.Certifications.Any())
            {
                profile.Certifications = new List<Certification>();
                foreach (var cert in viewModel.Certifications.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    profile.Certifications.Add(new Certification { Name = cert });
                }
            }
            
            // Save Achievements
            if (viewModel.Achievements != null && viewModel.Achievements.Any())
            {
                profile.Achievements = new List<Achievement>();
                foreach (var ach in viewModel.Achievements.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    profile.Achievements.Add(new Achievement { Description = ach });
                }
            }
            
            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"SaveChanges result: {result} records saved");
            
            return MapToViewModel(profile);
        }
        
        private UserProfileViewModel MapToViewModel(UserProfile profile)
        {
            var viewModel = new UserProfileViewModel
            {
                Id = profile.Id,
                FullName = profile.FullName,
                Email = profile.Email,
                Phone = profile.Phone,
                LinkedIn = profile.LinkedIn,
                GitHub = profile.GitHub,
                Languages = profile.Languages,
                Concepts = profile.Concepts,
                Software = profile.Software,
                Education = new List<EducationItemViewModel>(),
                Experience = new List<ExperienceItemViewModel>(),
                Projects = new List<ProjectItemViewModel>(),
                Certifications = new List<string>(),
                Achievements = new List<string>()
            };
            
            // Map Education
            if (profile.Education != null)
            {
                foreach (var edu in profile.Education)
                {
                    viewModel.Education.Add(new EducationItemViewModel
                    {
                        Id = edu.Id,
                        Degree = edu.Degree,
                        Institution = edu.Institution,
                        Duration = edu.Duration,
                        Highlights = string.IsNullOrEmpty(edu.Highlights) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(edu.Highlights) ?? new List<string>()
                    });
                }
            }
            
            // Map Experience
            if (profile.Experience != null)
            {
                foreach (var exp in profile.Experience)
                {
                    viewModel.Experience.Add(new ExperienceItemViewModel
                    {
                        Id = exp.Id,
                        Company = exp.Company,
                        Role = exp.Role,
                        Location = exp.Location,
                        Duration = exp.Duration,
                        Highlights = string.IsNullOrEmpty(exp.Highlights) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(exp.Highlights) ?? new List<string>()
                    });
                }
            }
            
            // Map Projects
            if (profile.Projects != null)
            {
                foreach (var proj in profile.Projects)
                {
                    viewModel.Projects.Add(new ProjectItemViewModel
                    {
                        Id = proj.Id,
                        Title = proj.Title,
                        Duration = proj.Duration,
                        Highlights = string.IsNullOrEmpty(proj.Highlights) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(proj.Highlights) ?? new List<string>()
                    });
                }
            }
            
            // Map Certifications
            if (profile.Certifications != null)
            {
                foreach (var cert in profile.Certifications)
                {
                    if (!string.IsNullOrWhiteSpace(cert.Name))
                        viewModel.Certifications.Add(cert.Name);
                }
            }
            
            // Map Achievements
            if (profile.Achievements != null)
            {
                foreach (var ach in profile.Achievements)
                {
                    if (!string.IsNullOrWhiteSpace(ach.Description))
                        viewModel.Achievements.Add(ach.Description);
                }
            }
            
            return viewModel;
        }
    }
}