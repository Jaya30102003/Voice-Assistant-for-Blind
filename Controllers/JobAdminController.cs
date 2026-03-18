using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Controllers
{
    [Authorize] // Require authentication for all actions
    public class JobAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobAdminController> _logger;
        
        public JobAdminController(AppDbContext context, ILogger<JobAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // GET: JobAdmin
        public async Task<IActionResult> Index()
        {
            try
            {
                var jobs = await _context.Jobs
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync();
                
                return View(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching jobs list");
                TempData["ErrorMessage"] = "An error occurred while loading jobs.";
                return View(new List<Job>());
            }
        }
        
        // GET: JobAdmin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var job = await _context.Jobs
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (job == null)
                {
                    return NotFound();
                }

                return View(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job details for ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading job details.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: JobAdmin/Create
        public IActionResult Create()
        {
            return View(new JobViewModel());
        }
        
        // POST: JobAdmin/Create (UPDATED with auto-generated Job Code)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobViewModel model)
        {
            Console.WriteLine("========== CREATE JOB ATTEMPT ==========");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state?.Errors != null && state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            Console.WriteLine($"- {key}: {error.ErrorMessage}");
                        }
                    }
                }
                return View(model);
            }
            
            try
            {
                // AUTO-GENERATE JOB CODE STARTING FROM VAJ0001
                var lastJob = await _context.Jobs
                    .OrderByDescending(j => j.Id)
                    .FirstOrDefaultAsync();
                
                string newJobCode;
                if (lastJob == null)
                {
                    // First job - start with VAJ0001
                    newJobCode = "VAJ0001";
                }
                else
                {
                    // Extract number from last job code and increment
                    var lastCode = lastJob.JobCode;
                    if (lastCode.StartsWith("VAJ") && lastCode.Length > 3)
                    {
                        var numberPart = lastCode.Substring(3);
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            int newNumber = lastNumber + 1;
                            newJobCode = $"VAJ{newNumber:D4}"; // D4 ensures 4 digits with leading zeros
                        }
                        else
                        {
                            // Fallback if format is unexpected
                            newJobCode = "VAJ0001";
                        }
                    }
                    else
                    {
                        newJobCode = "VAJ0001";
                    }
                }
                
                // Check if generated code already exists (safety check)
                while (await _context.Jobs.AnyAsync(j => j.JobCode == newJobCode))
                {
                    var numberPart = newJobCode.Substring(3);
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        newJobCode = $"VAJ{currentNumber + 1:D4}";
                    }
                    else
                    {
                        newJobCode = "VAJ0001";
                    }
                }
                
                var job = new Job
                {
                    JobCode = newJobCode, // Use auto-generated code
                    CompanyName = model.CompanyName?.Trim(),
                    JobTitle = model.JobTitle?.Trim(),
                    RequiredSkills = model.RequiredSkills?.Trim(),
                    Description = model.Description?.Trim(),
                    LastDateToApply = model.LastDateToApply,
                    HREmail = model.HREmail?.Trim().ToLower(),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Job created successfully: {job.JobCode} - {job.JobTitle}");
                TempData["SuccessMessage"] = $"Job '{job.JobCode}' created successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                ModelState.AddModelError("", "An error occurred while creating the job. Please try again.");
                return View(model);
            }
        }
        
        // GET: JobAdmin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return NotFound();
                }
                
                var model = new JobViewModel
                {
                    Id = job.Id,
                    JobCode = job.JobCode,
                    CompanyName = job.CompanyName,
                    JobTitle = job.JobTitle,
                    RequiredSkills = job.RequiredSkills,
                    Description = job.Description,
                    LastDateToApply = job.LastDateToApply,
                    HREmail = job.HREmail,
                    IsActive = job.IsActive
                };
                
                // Pass metadata to view
                ViewBag.CreatedAt = job.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                ViewBag.UpdatedAt = job.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not updated yet";
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the job for editing.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // POST: JobAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return NotFound();
                }
                
                // Check if JobCode is being changed and if it already exists
                if (job.JobCode != model.JobCode)
                {
                    var existingJob = await _context.Jobs
                        .FirstOrDefaultAsync(j => j.JobCode == model.JobCode && j.Id != id);
                    
                    if (existingJob != null)
                    {
                        ModelState.AddModelError("JobCode", "Job Code already exists. Please use a different code.");
                        return View(model);
                    }
                }
                
                // Update job properties
                job.JobCode = model.JobCode?.ToUpper().Trim();
                job.CompanyName = model.CompanyName?.Trim();
                job.JobTitle = model.JobTitle?.Trim();
                job.RequiredSkills = model.RequiredSkills?.Trim();
                job.Description = model.Description?.Trim();
                job.LastDateToApply = model.LastDateToApply;
                job.HREmail = model.HREmail?.Trim().ToLower();
                job.IsActive = model.IsActive;
                job.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Job updated successfully: {job.JobCode}");
                TempData["SuccessMessage"] = $"Job '{job.JobCode}' updated successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating job ID: {Id}", id);
                
                if (!await JobExistsAsync(id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "The job was modified by another user. Please refresh and try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job ID: {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the job. Please try again.");
                return View(model);
            }
        }
        
        // POST: JobAdmin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return NotFound();
                }
                
                var jobCode = job.JobCode;
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Job deleted successfully: {jobCode}");
                TempData["SuccessMessage"] = $"Job '{jobCode}' deleted successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the job. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: JobAdmin/ActiveJobs (Filter active jobs)
        public async Task<IActionResult> ActiveJobs()
        {
            try
            {
                var jobs = await _context.Jobs
                    .Where(j => j.IsActive && j.LastDateToApply >= DateTime.Today)
                    .OrderBy(j => j.LastDateToApply)
                    .ToListAsync();
                
                ViewBag.Filter = "Active Jobs";
                return View("Index", jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active jobs");
                TempData["ErrorMessage"] = "An error occurred while loading active jobs.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: JobAdmin/ExpiredJobs (Filter expired jobs)
        public async Task<IActionResult> ExpiredJobs()
        {
            try
            {
                var jobs = await _context.Jobs
                    .Where(j => !j.IsActive || j.LastDateToApply < DateTime.Today)
                    .OrderByDescending(j => j.LastDateToApply)
                    .ToListAsync();
                
                ViewBag.Filter = "Expired/Inactive Jobs";
                return View("Index", jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expired jobs");
                TempData["ErrorMessage"] = "An error occurred while loading expired jobs.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: JobAdmin/Search
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return RedirectToAction(nameof(Index));
                }
                
                var jobs = await _context.Jobs
                    .Where(j => 
                        j.JobCode.Contains(searchTerm) ||
                        j.CompanyName.Contains(searchTerm) ||
                        j.JobTitle.Contains(searchTerm) ||
                        j.RequiredSkills.Contains(searchTerm) ||
                        j.Description.Contains(searchTerm))
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync();
                
                ViewBag.SearchTerm = searchTerm;
                ViewBag.Filter = $"Search Results for '{searchTerm}'";
                
                return View("Index", jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs with term: {SearchTerm}", searchTerm);
                TempData["ErrorMessage"] = "An error occurred while searching jobs.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: JobAdmin/Stats (Dashboard with job statistics)
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var totalJobs = await _context.Jobs.CountAsync();
                var activeJobs = await _context.Jobs.CountAsync(j => j.IsActive && j.LastDateToApply >= DateTime.Today);
                var expiredJobs = await _context.Jobs.CountAsync(j => !j.IsActive || j.LastDateToApply < DateTime.Today);
                
                var jobsByCompany = await _context.Jobs
                    .GroupBy(j => j.CompanyName)
                    .Select(g => new { Company = g.Key ?? "Unknown", Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();
                
                ViewBag.TotalJobs = totalJobs;
                ViewBag.ActiveJobs = activeJobs;
                ViewBag.ExpiredJobs = expiredJobs;
                ViewBag.JobsByCompany = jobsByCompany;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job statistics");
                TempData["ErrorMessage"] = "An error occurred while loading statistics.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Helper method to check if job exists
        private async Task<bool> JobExistsAsync(int id)
        {
            return await _context.Jobs.AnyAsync(e => e.Id == id);
        }
    }
}