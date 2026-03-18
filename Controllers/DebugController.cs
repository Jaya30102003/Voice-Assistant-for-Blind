// Controllers/DebugController.cs
using Microsoft.AspNetCore.Mvc;
using VoiceAssistantForBlind.Data;
using Microsoft.EntityFrameworkCore;

namespace VoiceAssistantForBlind.Controllers
{
    public class DebugController : Controller
    {
        private readonly AppDbContext _context;
        
        public DebugController(AppDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> CheckDatabase()
        {
            var profiles = await _context.UserProfiles
                .Include(p => p.Education)
                .Include(p => p.Experience)
                .Include(p => p.Projects)
                .Include(p => p.Certifications)
                .Include(p => p.Achievements)
                .ToListAsync();
            
            var output = "=== DATABASE CONTENTS ===\n\n";
            output += $"Total profiles: {profiles.Count}\n\n";
            
            foreach (var profile in profiles)
            {
                output += $"Profile ID: {profile.Id}\n";
                output += $"Name: {profile.FullName}\n";
                output += $"Email: {profile.Email}\n";
                output += $"Created: {profile.CreatedAt}\n";
                output += $"Updated: {profile.UpdatedAt}\n";
                output += $"Education count: {profile.Education?.Count ?? 0}\n";
                output += $"Experience count: {profile.Experience?.Count ?? 0}\n";
                output += $"Projects count: {profile.Projects?.Count ?? 0}\n";
                output += $"Certifications count: {profile.Certifications?.Count ?? 0}\n";
                output += $"Achievements count: {profile.Achievements?.Count ?? 0}\n";
                output += "-------------------\n";
            }
            
            return Content(output, "text/plain");
        }
    }
}