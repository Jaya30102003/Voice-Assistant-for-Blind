// Controllers/AdminApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize] // Require valid JWT token
    public class AdminApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminApiController> _logger;
        
        public AdminApiController(AppDbContext context, ILogger<AdminApiController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // GET: api/admin/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get user info from JWT claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            return Ok(new
            {
                userId,
                username,
                email,
                role,
                message = "This is a protected endpoint accessible only with valid JWT"
            });
        }
        
        // GET: api/admin/jobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _context.Jobs
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            
            return Ok(jobs);
        }
        
        // POST: api/admin/jobs
        [HttpPost("jobs")]
        public async Task<IActionResult> CreateJob([FromBody] JobViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            try
            {
                var job = new Job
                {
                    JobCode = model.JobCode,
                    CompanyName = model.CompanyName,
                    JobTitle = model.JobTitle,
                    RequiredSkills = model.RequiredSkills,
                    Description = model.Description,
                    LastDateToApply = model.LastDateToApply,
                    HREmail = model.HREmail,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Job created successfully", jobId = job.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                return StatusCode(500, new { error = "Failed to create job" });
            }
        }
    }
}