using Microsoft.AspNetCore.Mvc;
using VoiceAssistantForBlind.Services;
using VoiceAssistantForBlind.Models;
using System.Text.Json;
using NerApi.Models;
using NerApi.Services;
using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VoiceAssistantForBlind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceCommandController : ControllerBase
    {
        private readonly IWhisperTranscriptionService _sttService;
        private readonly ProfileService _profileService;
        private readonly ResumePdfService _resumePdfService;
        private readonly AppDbContext _context;
        private readonly ILogger<VoiceCommandController> _logger;
        private readonly IEmailService _emailService;
        
        // In-memory storage for job listings by session
        private static readonly Dictionary<string, List<Job>> _lastJobListings = new();
        private static readonly Dictionary<string, bool> _awaitingJobSelection = new();
        private static readonly Dictionary<string, Job> _selectedJobs = new();

        public VoiceCommandController(
            IWhisperTranscriptionService sttService,
            ProfileService profileService,
            ResumePdfService resumePdfService,
            AppDbContext context,
            ILogger<VoiceCommandController> logger,
            IEmailService emailService)
        {
            _sttService = sttService;
            _profileService = profileService;
            _resumePdfService = resumePdfService;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        private string GetSessionId()
        {
            // Use IP address as simple session identifier
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "default";
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessVoiceCommand(IFormFile audio)
        {
            Console.WriteLine("\n========== VOICE COMMAND RECEIVED ==========");
            
            try
            {
                Console.WriteLine($"Audio file: {audio.FileName}, Size: {audio.Length} bytes");
                
                using var memoryStream = new MemoryStream();
                await audio.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                Console.WriteLine("Calling STT service...");
                var text = await _sttService.TranscribeAsync(memoryStream);
                
                Console.WriteLine($"✅ Recognized text: '{text}'");
                
                // ===== INTERVIEW COMMAND INTERCEPTOR =====
                // Check for interview commands FIRST before anything else
                if (text.Contains("start interview", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("mock interview", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("practice interview", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("begin interview", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("interview me", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ Interview command detected! Forwarding to InterviewController...");
                    
                    // Extract topic - FIXED VERSION with punctuation removal
                    string topic = "general";
                    
                    // Clean the text by removing punctuation
                    string cleanText = text.Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "");
                    
                    // Check for specific topics in the cleaned text
                    if (cleanText.Contains("c#", StringComparison.OrdinalIgnoreCase) || 
                        cleanText.Contains("c sharp", StringComparison.OrdinalIgnoreCase))
                        topic = "C#";
                    else if (cleanText.Contains("python", StringComparison.OrdinalIgnoreCase))
                        topic = "Python";
                    else if (cleanText.Contains("sql", StringComparison.OrdinalIgnoreCase))
                        topic = "SQL";
                    else if (cleanText.Contains("java", StringComparison.OrdinalIgnoreCase))
                        topic = "Java";
                    else if (cleanText.Contains("javascript", StringComparison.OrdinalIgnoreCase) || 
                             cleanText.Contains("js", StringComparison.OrdinalIgnoreCase))
                        topic = "JavaScript";
                    else
                    {
                        // Try to extract topic after "interview" using regex
                        var match = Regex.Match(text, @"interview\s+(?:on\s+)?([a-zA-Z#+.]+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            topic = match.Groups[1].Value.TrimEnd('.', ',', '?', '!', ' ', '\t', ';', ':');
                        }
                    }
                    
                    // Final cleanup - remove any remaining punctuation
                    topic = topic.TrimEnd('.', ',', '?', '!', ' ', '\t', ';', ':');
                    
                    Console.WriteLine($"✅ Extracted topic: '{topic}'");
                    
                    // Get services needed for InterviewController
                    var interviewService = HttpContext.RequestServices.GetRequiredService<InterviewService>();
                    
                    // Create a logger for InterviewController
                    var interviewLogger = HttpContext.RequestServices.GetRequiredService<ILogger<InterviewController>>();
                    
                    // Forward to InterviewController with correct logger type
                    var interviewController = new InterviewController(
                        interviewService,
                        _sttService,
                        interviewLogger, // Use the correct logger type
                        _context
                    );
                    
                    // Copy controller context
                    interviewController.ControllerContext = new ControllerContext
                    {
                        HttpContext = HttpContext
                    };
                    
                    return await interviewController.StartInterview(new StartInterviewRequest { Topic = topic });
                }
                // ===== END OF INTERVIEW COMMAND INTERCEPTOR =====
                
                var sessionId = GetSessionId();
                
                // FIRST: Check for application commands (these should work even without selection mode)
                if (_selectedJobs.ContainsKey(sessionId))
                {
                    if (text.Contains("apply for this job", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("apply now", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("send application", StringComparison.OrdinalIgnoreCase) ||
                        (text.Contains("apply") && text.Contains("job")))
                    {
                        Console.WriteLine("✅ Application command detected for selected job!");
                        return await SendJobApplicationEmail(_selectedJobs[sessionId]);
                    }
                }
                
                // SECOND: Check if we're in job selection mode
                var selectionResult = await HandleJobSelection(text);
                if (selectionResult != null)
                {
                    return selectionResult;
                }
                
                // Check for job listing commands
                if (text.Contains("list jobs", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("available jobs", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("show jobs", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("job openings", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("vacancies", StringComparison.OrdinalIgnoreCase) ||
                    (text.Contains("jobs") && text.Contains("available")))
                {
                    Console.WriteLine("✅ Job listing command detected!");
                    return await ListAvailableJobs();
                }
                
                // Check for resume/profile commands
                if (text.Contains("resume", StringComparison.OrdinalIgnoreCase) || 
                    text.Contains("profile", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("generate", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("create", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("make", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("build", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✅ Resume/profile command detected!");
                    return await GenerateResume();
                }
                
                // Check for help command
                if (text.Contains("help", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("what can I say", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("instructions", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { 
                        message = "You can say: 'list jobs' to hear available jobs, " +
                                  "'select job' followed by a number to choose a job, " +
                                  "'apply for this job' to send your resume, " +
                                  "'generate resume' to create your PDF resume, " +
                                  "or 'start interview' to practice for interviews."
                    });
                }
                
                return Ok(new { message = $"I heard: '{text}'. Try saying 'list jobs' or 'generate resume'." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForJob([FromForm] string hrEmail, [FromForm] string jobTitle, [FromForm] string company, [FromForm] string jobCode)
        {
            try
            {
                _logger.LogInformation($"Apply for job: {jobTitle} at {company}, HR: {hrEmail}");
                
                // Validate required fields
                if (string.IsNullOrEmpty(hrEmail) || string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(company))
                {
                    return Ok(new { 
                        success = false, 
                        message = "Missing job information. Please select a job first.",
                        requiresProfile = false
                    });
                }

                // Send email using email service
                var result = await _emailService.SendJobApplication(hrEmail, jobTitle, company, jobCode ?? "");
                
                return Ok(new { 
                    success = result.Success,
                    message = result.Message,
                    requiresProfile = result.RequiresProfile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplyForJob endpoint");
                return Ok(new { 
                    success = false, 
                    message = "An error occurred while sending your application. Please try again.",
                    requiresProfile = false
                });
            }
        }

        private async Task<IActionResult?> HandleJobSelection(string text)
        {
            var sessionId = GetSessionId();
            
            // Check if we're in selection mode
            if (!_awaitingJobSelection.ContainsKey(sessionId) || !_awaitingJobSelection[sessionId])
            {
                return null; // Not in selection mode
            }
            
            Console.WriteLine("🔍 In job selection mode, processing input...");
            
            // Check if user wants to cancel
            if (text.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("nothing", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("exit", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("no thanks", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("no job", StringComparison.OrdinalIgnoreCase))
            {
                _awaitingJobSelection[sessionId] = false;
                return Ok(new { message = "Okay, no problem. Say 'list jobs' anytime to hear available positions again." });
            }
            
            // Try to extract job number
            int? jobNumber = null;
            
            // Pattern matching for job numbers
            if (text.Contains("first", StringComparison.OrdinalIgnoreCase) || 
                (text.Contains("job") && text.Contains("1", StringComparison.OrdinalIgnoreCase)))
                jobNumber = 1;
            else if (text.Contains("second", StringComparison.OrdinalIgnoreCase) || 
                     (text.Contains("job") && text.Contains("2", StringComparison.OrdinalIgnoreCase)))
                jobNumber = 2;
            else if (text.Contains("third", StringComparison.OrdinalIgnoreCase) || 
                     (text.Contains("job") && text.Contains("3", StringComparison.OrdinalIgnoreCase)))
                jobNumber = 3;
            else if (text.Contains("fourth", StringComparison.OrdinalIgnoreCase) || 
                     (text.Contains("job") && text.Contains("4", StringComparison.OrdinalIgnoreCase)))
                jobNumber = 4;
            else if (text.Contains("fifth", StringComparison.OrdinalIgnoreCase) || 
                     (text.Contains("job") && text.Contains("5", StringComparison.OrdinalIgnoreCase)))
                jobNumber = 5;
            else
            {
                // Try to extract any number from the text
                var match = Regex.Match(text, @"\d+");
                if (match.Success)
                {
                    jobNumber = int.Parse(match.Value);
                }
            }
            
            if (!jobNumber.HasValue)
            {
                return Ok(new { 
                    message = "I didn't catch which job you want. Please say 'select job 1', 'job 2', etc., or say 'cancel' to exit.",
                    isSelectionPrompt = true 
                });
            }
            
            // Get stored jobs
            if (!_lastJobListings.ContainsKey(sessionId))
            {
                _awaitingJobSelection[sessionId] = false;
                return Ok(new { message = "I don't have any recent job listings. Please say 'list jobs' to hear available positions." });
            }
            
            var jobs = _lastJobListings[sessionId];
            
            if (jobNumber < 1 || jobNumber > jobs.Count)
            {
                return Ok(new { 
                    message = $"There is no job number {jobNumber}. Please select a number between 1 and {jobs.Count}.",
                    isSelectionPrompt = true 
                });
            }
            
            // Get the selected job
            var selectedJob = jobs[jobNumber.Value - 1];
            
            // Store selected job for this session
            _selectedJobs[sessionId] = selectedJob;
            
            // Clear selection mode
            _awaitingJobSelection[sessionId] = false;
            
            // Format response with HR email
            string response = $"You selected {selectedJob.JobTitle} at {selectedJob.CompanyName}. ";
            response += $"The HR email address is: {selectedJob.HREmail}. ";
            response += $"To apply for this job, say 'apply for this job'. ";
            response += $"Or say 'list jobs' to hear other positions.";
            
            Console.WriteLine($"✅ Job selected: {selectedJob.JobCode} - HR Email: {selectedJob.HREmail}");
            
            return Ok(new { 
                message = response,
                hrEmail = selectedJob.HREmail,
                jobTitle = selectedJob.JobTitle,
                company = selectedJob.CompanyName,
                jobCode = selectedJob.JobCode,
                canApply = true
            });
        }

        private async Task<IActionResult> ListAvailableJobs()
        {
            Console.WriteLine("\n========== LISTING AVAILABLE JOBS ==========");
            
            try
            {
                // Get active jobs from database (not expired and active)
                var jobs = await _context.Jobs
                    .Where(j => j.IsActive && j.LastDateToApply >= DateTime.Today)
                    .OrderBy(j => j.LastDateToApply)
                    .Take(5) // Limit to 5 jobs to avoid speaking too much
                    .ToListAsync();
                
                if (!jobs.Any())
                {
                    Console.WriteLine("No active jobs found");
                    return Ok(new { message = "No active jobs are available at the moment. Please check back later." });
                }
                
                // Store jobs for this session
                var sessionId = GetSessionId();
                _lastJobListings[sessionId] = jobs;
                _awaitingJobSelection[sessionId] = true;
                
                Console.WriteLine($"Found {jobs.Count} active jobs, waiting for selection...");
                
                // Format jobs for speech
                string message;
                if (jobs.Count == 1)
                {
                    var job = jobs.First();
                    message = $"There is 1 job available. Job 1: {job.JobTitle} at {job.CompanyName}. " +
                              $"Skills required: {job.RequiredSkills}. " +
                              $"Last date to apply is {job.LastDateToApply:MMMM dd, yyyy}. " +
                              $"To get the HR email, say 'select job 1' or say 'cancel' to exit.";
                }
                else
                {
                    message = $"I found {jobs.Count} available jobs. ";
                    int index = 1;
                    foreach (var job in jobs)
                    {
                        message += $"Job {index}: {job.JobTitle} at {job.CompanyName}. ";
                        index++;
                    }
                    message += $"To get HR email for a specific job, say 'select job' followed by the number. For example, 'select job 1'. Or say 'cancel' to exit.";
                }
                
                Console.WriteLine($"✅ Returning job listings");
                return Ok(new { 
                    message,
                    hasJobs = true,
                    jobCount = jobs.Count,
                    awaitingSelection = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing jobs: {ex.Message}");
                return Ok(new { message = "Sorry, I encountered an error while fetching job listings." });
            }
        }

        private async Task<IActionResult> SendJobApplicationEmail(Job selectedJob)
        {
            try
            {
                _logger.LogInformation($"Sending application for {selectedJob.JobTitle} at {selectedJob.CompanyName}");
                
                // Get user profile first to check if it exists
                var profile = await _profileService.GetLatestProfileAsync();
                if (profile == null)
                {
                    return Ok(new { 
                        message = "Please create your profile first. Click the 'Update Profile' button in the top right corner, or say 'generate resume' to get started.",
                        requiresProfile = true,
                        success = false
                    });
                }

                var result = await _emailService.SendJobApplication(
                    selectedJob.HREmail,
                    selectedJob.JobTitle,
                    selectedJob.CompanyName,
                    selectedJob.JobCode
                );

                if (result.Success)
                {
                    // Clear the selected job after successful application
                    var sessionId = GetSessionId();
                    if (_selectedJobs.ContainsKey(sessionId))
                    {
                        _selectedJobs.Remove(sessionId);
                    }
                }

                return Ok(new { 
                    message = result.Message,
                    success = result.Success,
                    requiresProfile = result.RequiresProfile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application email");
                return Ok(new { 
                    message = "Sorry, I encountered an error while sending your application. Please try again.",
                    success = false,
                    requiresProfile = false
                });
            }
        }

        private async Task<IActionResult> GenerateResume()
        {
            Console.WriteLine("\n========== GENERATING RESUME ==========");
            
            try
            {
                // Get profile from database (returns UserProfileViewModel)
                var profile = await _profileService.GetLatestProfileAsync();
                
                if (profile == null)
                {
                    return Ok(new { message = "No profile found. Please fill out your information at /Profile/Edit first." });
                }

                Console.WriteLine($"✅ Profile found for: {profile.FullName}");

                // Create resume request with ALL profile data
                var resumeRequest = new ResumeRequest
                {
                    FullName = profile.FullName,
                    Email = profile.Email,
                    Phone = profile.Phone,
                    LinkedIn = profile.LinkedIn,
                    GitHub = profile.GitHub,
                    Languages = profile.Languages,
                    Concepts = profile.Concepts,
                    Software = profile.Software,
                    
                    Education = profile.Education?.Select(e => new EducationItem
                    {
                        Degree = e.Degree,
                        Institution = e.Institution,
                        Duration = e.Duration,
                        Highlights = e.Highlights != null && e.Highlights.Any() 
                            ? JsonSerializer.Serialize(e.Highlights) 
                            : null
                    }).ToList() ?? new List<EducationItem>(),
                    
                    Experience = profile.Experience?.Select(e => new ExperienceItem
                    {
                        Company = e.Company,
                        Role = e.Role,
                        Location = e.Location,
                        Duration = e.Duration,
                        Highlights = e.Highlights != null && e.Highlights.Any() 
                            ? JsonSerializer.Serialize(e.Highlights) 
                            : null
                    }).ToList() ?? new List<ExperienceItem>(),
                    
                    Projects = profile.Projects?.Select(p => new ProjectItem
                    {
                        Title = p.Title,
                        Duration = p.Duration,
                        Highlights = p.Highlights != null && p.Highlights.Any() 
                            ? JsonSerializer.Serialize(p.Highlights) 
                            : null
                    }).ToList() ?? new List<ProjectItem>(),
                    
                    Certifications = profile.Certifications ?? new List<string>(),
                    Achievements = profile.Achievements ?? new List<string>()
                };

                Console.WriteLine("Generating PDF...");
                var pdfBytes = _resumePdfService.GeneratePdf(resumeRequest);
                
                var fileName = $"Resume_{profile.FullName?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                
                Console.WriteLine($"✅ PDF generated! Size: {pdfBytes.Length} bytes");
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return Ok(new { message = $"Error generating resume: {ex.Message}" });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "VoiceCommandController is working!" });
        }
    }
}
