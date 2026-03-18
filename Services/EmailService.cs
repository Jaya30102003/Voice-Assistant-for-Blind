using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Models.ViewModels;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace VoiceAssistantForBlind.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ProfileService _profileService;
        private readonly ResumePdfService _resumePdfService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration config,
            ProfileService profileService,
            ResumePdfService resumePdfService,
            ILogger<EmailService> logger)
        {
            _config = config;
            _profileService = profileService;
            _resumePdfService = resumePdfService;
            _logger = logger;
        }

        public async Task<EmailSendResult> SendJobApplication(string hrEmail, string jobTitle, string company, string jobCode)
        {
            try
            {
                _logger.LogInformation($"Starting email for {jobTitle} at {company}");

                // Validate inputs
                if (string.IsNullOrEmpty(hrEmail) || string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(company))
                {
                    return new EmailSendResult 
                    { 
                        Success = false, 
                        Message = "Invalid job information provided." 
                    };
                }

                // 1. Get user profile
                var profile = await _profileService.GetLatestProfileAsync();
                if (profile == null)
                {
                    return new EmailSendResult 
                    { 
                        Success = false, 
                        Message = "No profile found. Please update your profile first." 
                    };
                }

                // 2. Generate fresh resume with proper type conversion
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

                _logger.LogInformation("Generating PDF resume...");
                var pdfBytes = _resumePdfService.GeneratePdf(resumeRequest);
                
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return new EmailSendResult 
                    { 
                        Success = false, 
                        Message = "Failed to generate resume PDF." 
                    };
                }

                // 3. Create and send email
                using (var message = new MailMessage())
                {
                    // Configure email
                    var senderEmail = _config["Email:SenderEmail"];
                    var senderName = _config["Email:SenderName"] ?? "Voice Assistant for Blind";
                    
                    if (string.IsNullOrEmpty(senderEmail))
                    {
                        return new EmailSendResult 
                        { 
                            Success = false, 
                            Message = "Email configuration is missing. Please contact support." 
                        };
                    }

                    message.From = new MailAddress(senderEmail, senderName);
                    message.To.Add(hrEmail);
                    
                    // Add CC to user if email is valid
                    if (!string.IsNullOrEmpty(profile.Email) && profile.Email.Contains("@"))
                    {
                        message.CC.Add(profile.Email); // Send copy to user
                    }
                    
                    message.Subject = $"Job Application: {jobTitle} at {company}";
                    
                    // Professional email body
                    message.Body = $@"
Dear Hiring Team,

I am writing to apply for the {jobTitle} position at {company} (Job Code: {jobCode ?? "N/A"}).

My resume is attached to this email. Here's a brief overview of my qualifications:

Full Name: {profile.FullName}
Email: {profile.Email}
Phone: {profile.Phone ?? "Not provided"}

Key Skills:
- Languages: {profile.Languages ?? "Not specified"}
- Concepts: {profile.Concepts ?? "Not specified"}
- Software: {profile.Software ?? "Not specified"}

I am very interested in this opportunity and believe my skills would be valuable to your team.

Thank you for your time and consideration.

Best regards,
{profile.FullName}
{profile.Email}
{profile.Phone ?? ""}

---
Sent via Voice Assistant for Blind - Making job applications accessible to everyone.
";
                    
                    message.IsBodyHtml = false; // Plain text for better compatibility

                    // Attach resume PDF
                    var fileName = $"Resume_{profile.FullName?.Replace(" ", "_") ?? "Candidate"}.pdf";
                    var attachment = new Attachment(new MemoryStream(pdfBytes), fileName, "application/pdf");
                    message.Attachments.Add(attachment);

                    // Configure SMTP client with better timeout handling
                    var smtpServer = _config["Email:SmtpServer"] ?? "smtp.gmail.com";
                    var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                    var enableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");
                    var appPassword = _config["Email:AppPassword"];

                    if (string.IsNullOrEmpty(appPassword))
                    {
                        return new EmailSendResult 
                        { 
                            Success = false, 
                            Message = "Email password not configured. Please contact support." 
                        };
                    }

                    _logger.LogInformation($"Attempting to connect to SMTP server {smtpServer}:{smtpPort} with SSL={enableSsl}");
                    
                    using (var client = new SmtpClient(smtpServer, smtpPort))
                    {
                        client.EnableSsl = enableSsl;
                        client.Timeout = 60000; // 60 seconds timeout (increased from default)
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(senderEmail, appPassword);
                        
                        _logger.LogInformation("Sending email to {HrEmail}...", hrEmail);
                        
                        try
                        {
                            await client.SendMailAsync(message);
                            _logger.LogInformation($"Email sent successfully to {hrEmail}");
                        }
                        catch (SmtpException smtpEx)
                        {
                            _logger.LogError(smtpEx, "SMTP error sending email");
                            
                            // If using port 587 failed, try port 465 as fallback
                            if (smtpPort == 587)
                            {
                                _logger.LogInformation("Retrying with port 465...");
                                using (var fallbackClient = new SmtpClient(smtpServer, 465))
                                {
                                    fallbackClient.EnableSsl = true;
                                    fallbackClient.Timeout = 60000;
                                    fallbackClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                                    fallbackClient.UseDefaultCredentials = false;
                                    fallbackClient.Credentials = new NetworkCredential(senderEmail, appPassword);
                                    
                                    await fallbackClient.SendMailAsync(message);
                                    _logger.LogInformation($"Email sent successfully via port 465 to {hrEmail}");
                                }
                            }
                            else
                            {
                                throw; // Re-throw if not a port fallback scenario
                            }
                        }
                    }
                }

                return new EmailSendResult 
                { 
                    Success = true, 
                    Message = $"Application sent successfully to {company}! A copy has been sent to your email." 
                };
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email");
                
                // Provide more helpful error message based on the exception
                string errorMessage = "Failed to send email due to server error. ";
                
                if (smtpEx.Message.Contains("timed out") || smtpEx.InnerException?.Message?.Contains("timed out") == true)
                {
                    errorMessage = "Connection timed out. This might be due to firewall blocking port 587. Please try using port 465 in your configuration, or check your network settings.";
                }
                else if (smtpEx.Message.Contains("authentication") || smtpEx.Message.Contains("credentials"))
                {
                    errorMessage = "Authentication failed. Please check your email and app password.";
                }
                else if (smtpEx.Message.Contains("SSL") || smtpEx.Message.Contains("TLS"))
                {
                    errorMessage = "SSL/TLS error. Please check your SSL settings.";
                }
                else
                {
                    errorMessage += smtpEx.Message;
                }
                
                return new EmailSendResult 
                { 
                    Success = false, 
                    Message = errorMessage,
                    ErrorDetails = smtpEx.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email");
                return new EmailSendResult 
                { 
                    Success = false, 
                    Message = $"Failed to send application: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
        }
    }
}