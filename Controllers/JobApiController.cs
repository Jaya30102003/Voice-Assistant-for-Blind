using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobApiController> _logger;
        
        public JobApiController(AppDbContext context, ILogger<JobApiController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // GET: api/JobApi/available
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableJobs()
        {
            try
            {
                var jobs = await _context.Jobs
                    .Where(j => j.IsActive && j.LastDateToApply >= DateTime.Today)
                    .OrderBy(j => j.LastDateToApply)
                    .Select(j => new
                    {
                        j.Id,
                        j.JobCode,
                        j.CompanyName,
                        j.JobTitle,
                        j.RequiredSkills,
                        j.Description,
                        LastDate = j.LastDateToApply.ToString("yyyy-MM-dd"),
                        j.HREmail,
                        DaysRemaining = (j.LastDateToApply - DateTime.Today).Days
                    })
                    .ToListAsync();
                
                return Ok(new
                {
                    count = jobs.Count,
                    jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching jobs");
                return StatusCode(500, new { error = "An error occurred while fetching jobs" });
            }
        }
        
        // GET: api/JobApi/search?skills=c#,java
        [HttpGet("search")]
        public async Task<IActionResult> SearchJobs([FromQuery] string skills)
        {
            try
            {
                if (string.IsNullOrEmpty(skills))
                {
                    return await GetAvailableJobs();
                }
                
                var skillList = skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLower())
                    .ToList();
                
                var jobs = await _context.Jobs
                    .Where(j => j.IsActive && j.LastDateToApply >= DateTime.Today)
                    .ToListAsync();
                
                // Filter jobs that contain any of the requested skills
                var matchingJobs = jobs
                    .Where(j => !string.IsNullOrEmpty(j.RequiredSkills))
                    .Select(j => new
                    {
                        Job = j,
                        RequiredSkillList = j.RequiredSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToList()
                    })
                    .Where(x => x.RequiredSkillList.Any(s => skillList.Contains(s)))
                    .Select(x => new
                    {
                        x.Job.Id,
                        x.Job.JobCode,
                        x.Job.CompanyName,
                        x.Job.JobTitle,
                        x.Job.RequiredSkills,
                        x.Job.Description,
                        LastDate = x.Job.LastDateToApply.ToString("yyyy-MM-dd"),
                        x.Job.HREmail,
                        DaysRemaining = (x.Job.LastDateToApply - DateTime.Today).Days
                    })
                    .ToList();
                
                return Ok(new
                {
                    count = matchingJobs.Count,
                    jobs = matchingJobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs");
                return StatusCode(500, new { error = "An error occurred while searching jobs" });
            }
        }
        
        // GET: api/JobApi/code/ABC001
        [HttpGet("code/{jobCode}")]
        public async Task<IActionResult> GetJobByCode(string jobCode)
        {
            try
            {
                var job = await _context.Jobs
                    .Where(j => j.JobCode == jobCode && j.IsActive)
                    .Select(j => new
                    {
                        j.Id,
                        j.JobCode,
                        j.CompanyName,
                        j.JobTitle,
                        j.RequiredSkills,
                        j.Description,
                        LastDate = j.LastDateToApply.ToString("yyyy-MM-dd"),
                        j.HREmail
                    })
                    .FirstOrDefaultAsync();
                
                if (job == null)
                {
                    return NotFound(new { message = "Job not found" });
                }
                
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job by code");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }
}