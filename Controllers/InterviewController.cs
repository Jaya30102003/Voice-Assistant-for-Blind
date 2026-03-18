using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Services;
using VoiceAssistantForBlind.Data;
using NerApi.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VoiceAssistantForBlind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly InterviewService _interviewService;
        private readonly IWhisperTranscriptionService _sttService;
        private readonly ILogger<InterviewController> _logger;
        private readonly AppDbContext _context;

        // In-memory session storage (use distributed cache in production)
        private static readonly Dictionary<string, InterviewSessionData> _activeSessions = new();

        public InterviewController(
            InterviewService interviewService,
            IWhisperTranscriptionService sttService,
            ILogger<InterviewController> logger,
            AppDbContext context)
        {
            _interviewService = interviewService;
            _sttService = sttService;
            _logger = logger;
            _context = context;
        }

        private string GetSessionId()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "default";
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            try
            {
                var totalQuestions = await _context.InterviewQuestions.CountAsync();
                var topics = await _context.InterviewQuestions
                    .Select(q => q.Topic)
                    .Distinct()
                    .ToListAsync();
                
                var questionsByTopic = new Dictionary<string, int>();
                foreach (var topic in topics)
                {
                    var count = await _context.InterviewQuestions
                        .Where(q => q.Topic == topic)
                        .CountAsync();
                    questionsByTopic[topic] = count;
                }
                
                return Ok(new
                {
                    message = "Interview controller is working!",
                    databaseConnected = true,
                    totalQuestions = totalQuestions,
                    topics = topics,
                    questionsByTopic = questionsByTopic,
                    activeSessions = _activeSessions.Count
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    message = "Interview controller is working but database error",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // FIXED: Both GET and POST versions of reseed
        [HttpPost("reseed")]
        [HttpGet("reseed")] // Allow GET for browser access
        public async Task<IActionResult> ReseedQuestions()
        {
            try
            {
                // Remove existing questions
                var existingQuestions = await _context.InterviewQuestions.ToListAsync();
                if (existingQuestions.Any())
                {
                    _context.InterviewQuestions.RemoveRange(existingQuestions);
                    await _context.SaveChangesAsync();
                }
                
                // Add fresh questions
                var questions = new List<InterviewQuestion>
                {
                    // C# Questions
                    new InterviewQuestion { 
                        Topic = "C#", 
                        Question = "Explain what object-oriented programming means in C#.", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 1 
                    },
                    new InterviewQuestion { 
                        Topic = "C#", 
                        Question = "What are the differences between abstract classes and interfaces?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 2 
                    },
                    new InterviewQuestion { 
                        Topic = "C#", 
                        Question = "Explain the difference between 'out' and 'ref' parameters.", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 3 
                    },
                    new InterviewQuestion { 
                        Topic = "C#", 
                        Question = "What are async/await and how do they work?", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 4 
                    },
                    new InterviewQuestion { 
                        Topic = "C#", 
                        Question = "What is the difference between .NET Core and .NET Framework?", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 5 
                    },
                    
                    // Python Questions
                    new InterviewQuestion { 
                        Topic = "Python", 
                        Question = "What are Python decorators and how do you use them?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 1 
                    },
                    new InterviewQuestion { 
                        Topic = "Python", 
                        Question = "Explain the difference between lists and tuples.", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 2 
                    },
                    new InterviewQuestion { 
                        Topic = "Python", 
                        Question = "What is the Global Interpreter Lock (GIL)?", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 3 
                    },
                    new InterviewQuestion { 
                        Topic = "Python", 
                        Question = "How do you handle exceptions in Python?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 4 
                    },
                    new InterviewQuestion { 
                        Topic = "Python", 
                        Question = "What are list comprehensions and when should you use them?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 5 
                    },
                    
                    // SQL Questions
                    new InterviewQuestion { 
                        Topic = "SQL", 
                        Question = "Explain the difference between INNER JOIN and LEFT JOIN.", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 1 
                    },
                    new InterviewQuestion { 
                        Topic = "SQL", 
                        Question = "What is a primary key and how is it different from a unique key?", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 2 
                    },
                    new InterviewQuestion { 
                        Topic = "SQL", 
                        Question = "Explain the difference between WHERE and HAVING clauses.", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 3 
                    },
                    new InterviewQuestion { 
                        Topic = "SQL", 
                        Question = "What are indexes and when should you use them?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 4 
                    },
                    new InterviewQuestion { 
                        Topic = "SQL", 
                        Question = "What is database normalization?", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 5 
                    },
                    
                    // JavaScript Questions
                    new InterviewQuestion { 
                        Topic = "JavaScript", 
                        Question = "Explain the difference between let, const, and var.", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 1 
                    },
                    new InterviewQuestion { 
                        Topic = "JavaScript", 
                        Question = "What is closure in JavaScript and how does it work?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 2 
                    },
                    new InterviewQuestion { 
                        Topic = "JavaScript", 
                        Question = "Explain the event loop in JavaScript.", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 3 
                    },
                    new InterviewQuestion { 
                        Topic = "JavaScript", 
                        Question = "What are promises and how do they work?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 4 
                    },
                    new InterviewQuestion { 
                        Topic = "JavaScript", 
                        Question = "Explain the concept of hoisting.", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 5 
                    },
                    
                    // Java Questions
                    new InterviewQuestion { 
                        Topic = "Java", 
                        Question = "Explain the concept of inheritance in Java.", 
                        Difficulty = "Beginner", 
                        DisplayOrder = 1 
                    },
                    new InterviewQuestion { 
                        Topic = "Java", 
                        Question = "What is the difference between abstract class and interface in Java?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 2 
                    },
                    new InterviewQuestion { 
                        Topic = "Java", 
                        Question = "How does garbage collection work in Java?", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 3 
                    },
                    new InterviewQuestion { 
                        Topic = "Java", 
                        Question = "What are the access modifiers in Java?", 
                        Difficulty = "Intermediate", 
                        DisplayOrder = 4 
                    },
                    new InterviewQuestion { 
                        Topic = "Java", 
                        Question = "Explain multithreading in Java.", 
                        Difficulty = "Advanced", 
                        DisplayOrder = 5 
                    }
                };
                
                _context.InterviewQuestions.AddRange(questions);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = $"Successfully reseeded {questions.Count} interview questions!",
                    topics = questions.Select(q => q.Topic).Distinct().ToList(),
                    counts = questions.GroupBy(q => q.Topic)
                        .ToDictionary(g => g.Key, g => g.Count())
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartInterview([FromBody] StartInterviewRequest request)
        {
            try
            {
                _logger.LogInformation("Starting interview for topic: {Topic}", request.Topic);
                
                var topic = request.Topic;
                var questions = await _interviewService.GetQuestionsForTopicAsync(topic);

                if (!questions.Any())
                {
                    var availableTopics = await _interviewService.GetAvailableTopicsAsync();
                    var topicsList = availableTopics.Any() 
                        ? string.Join(", ", availableTopics) 
                        : "C#, Python, SQL, Java, JavaScript";
                    
                    var topicsArray = availableTopics.Any() 
                        ? availableTopics 
                        : new List<string> { "C#", "Python", "SQL", "Java", "JavaScript" };
                        
                    return Ok(new
                    {
                        message = $"Sorry, I don't have interview questions for {topic} yet. Available topics: {topicsList}",
                        availableTopics = topicsArray
                    });
                }

                var sessionId = GetSessionId();
                var userId = GetUserId() ?? 0;

                // Create database session first
                var dbSession = await _interviewService.CreateSessionAsync(userId, topic, questions);
                
                // Store session in memory
                _activeSessions[sessionId] = new InterviewSessionData
                {
                    SessionId = sessionId,
                    UserId = userId,
                    Topic = topic,
                    Questions = questions,
                    CurrentIndex = 0,
                    StartTime = DateTime.UtcNow,
                    SessionDbId = dbSession.Id
                };

                var firstQuestion = questions[0].Question;

                _logger.LogInformation("Interview started successfully for topic {Topic} with {Count} questions", 
                    topic, questions.Count);

                return Ok(new
                {
                    message = $"Starting {topic} interview. First question: {firstQuestion}",
                    question = firstQuestion,
                    questionIndex = 0,
                    totalQuestions = questions.Count,
                    topic = topic,
                    sessionId = sessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting interview for topic {Topic}", request?.Topic);
                return BadRequest(new { error = "Failed to start interview. Please try again." });
            }
        }

        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateAnswer(IFormFile audio, [FromForm] int questionIndex, [FromForm] string sessionId)
        {
            try
            {
                _logger.LogInformation("Evaluating answer for session {SessionId}, question {QuestionIndex}", 
                    sessionId, questionIndex);

                // Get session from memory
                if (!_activeSessions.TryGetValue(sessionId, out var session))
                {
                    _logger.LogWarning("Interview session {SessionId} not found or expired", sessionId);
                    return BadRequest(new { message = "Interview session not found or expired. Please start a new interview." });
                }

                if (questionIndex >= session.Questions.Count)
                {
                    _logger.LogWarning("Invalid question index {QuestionIndex} for session {SessionId}", 
                        questionIndex, sessionId);
                    return BadRequest(new { message = "Invalid question index" });
                }

                // Convert speech to text
                using var memoryStream = new MemoryStream();
                await audio.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                _logger.LogInformation("Transcribing answer for question {QuestionIndex}", questionIndex);
                var answerText = await _sttService.TranscribeAsync(memoryStream);

                if (string.IsNullOrWhiteSpace(answerText))
                {
                    return Ok(new
                    {
                        message = "I couldn't hear your answer. Please try again.",
                        retry = true
                    });
                }

                _logger.LogInformation("Answer transcribed: {AnswerText}", answerText);

                // Get current question
                var currentQuestion = session.Questions[questionIndex];

                // Evaluate with LLM
                _logger.LogInformation("Evaluating answer with Gemini for question: {Question}", 
                    currentQuestion.Question.Substring(0, Math.Min(30, currentQuestion.Question.Length)));
                
                var feedback = await _interviewService.EvaluateAnswerAsync(
                    currentQuestion, 
                    answerText, 
                    session.UserId);

                // Save to database
                await _interviewService.SaveAnswerAsync(
                    session.SessionDbId,
                    currentQuestion.Question,
                    answerText,
                    feedback);

                // Update session
                session.CurrentIndex = questionIndex + 1;

                // Prepare next question or completion
                if (questionIndex + 1 < session.Questions.Count)
                {
                    var nextQuestion = session.Questions[questionIndex + 1].Question;

                    _logger.LogInformation("Moving to next question {NextIndex} for session {SessionId}", 
                        questionIndex + 1, sessionId);

                    return Ok(new
                    {
                        message = feedback.Summary ?? "Answer evaluated.",
                        feedback = new
                        {
                            score = feedback.Score,
                            strengths = feedback.Strengths ?? new List<string>(),
                            improvements = feedback.Improvements ?? new List<string>(),
                            sampleBetterAnswer = feedback.SampleBetterAnswer,
                            keyConcepts = feedback.KeyConcepts,
                            missingConcepts = feedback.MissingConcepts
                        },
                        nextQuestion = nextQuestion,
                        questionIndex = questionIndex + 1,
                        totalQuestions = session.Questions.Count,
                        isComplete = false
                    });
                }
                else
                {
                    // Interview complete
                    _logger.LogInformation("Interview completed for session {SessionId}", sessionId);
                    _activeSessions.Remove(sessionId);

                    return Ok(new
                    {
                        message = "Interview complete! Great job practicing!",
                        feedback = new
                        {
                            score = feedback.Score,
                            strengths = feedback.Strengths ?? new List<string>(),
                            improvements = feedback.Improvements ?? new List<string>()
                        },
                        isComplete = true,
                        summary = new
                        {
                            totalQuestions = session.Questions.Count,
                            topic = session.Topic,
                            averageScore = await GetAverageScoreForSession(session.SessionDbId)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating answer for session {SessionId}", sessionId);
                return BadRequest(new { error = "Failed to evaluate answer. Please try again." });
            }
        }

        private async Task<double?> GetAverageScoreForSession(int sessionDbId)
        {
            try
            {
                var scores = await _context.InterviewAnswers
                    .Where(a => a.SessionId == sessionDbId)
                    .Select(a => a.Score)
                    .ToListAsync();
                    
                return scores.Any() ? scores.Average() : (double?)null;
            }
            catch
            {
                return null;
            }
        }

        [HttpPost("cancel")]
        public IActionResult CancelInterview([FromBody] CancelInterviewRequest request)
        {
            var sessionId = request.SessionId ?? GetSessionId();
            
            if (_activeSessions.Remove(sessionId))
            {
                _logger.LogInformation("Interview cancelled for session {SessionId}", sessionId);
                return Ok(new { message = "Interview cancelled." });
            }

            return Ok(new { message = "No active interview session." });
        }

        [HttpGet("topics")]
        public async Task<IActionResult> GetAvailableTopics()
        {
            try
            {
                var topics = await _interviewService.GetAvailableTopicsAsync();
                return Ok(new { topics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available topics");
                return BadRequest(new { error = "Failed to fetch topics" });
            }
        }

        [HttpGet("check-topics")]
        public async Task<IActionResult> CheckTopics()
        {
            try
            {
                var topics = await _interviewService.GetAvailableTopicsAsync();
                var counts = new Dictionary<string, int>();
                
                foreach (var topic in topics)
                {
                    var count = await _interviewService.GetQuestionCountForTopicAsync(topic);
                    counts[topic] = count;
                }
                
                return Ok(new
                {
                    topics = topics,
                    counts = counts,
                    totalQuestions = await _context.InterviewQuestions.CountAsync(),
                    message = $"Found {topics.Count()} topics with {await _context.InterviewQuestions.CountAsync()} total questions"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking topics");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetInterviewHistory()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                var sessions = await _context.InterviewSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .Select(s => new
                    {
                        s.Id,
                        s.Topic,
                        s.StartTime,
                        s.TotalQuestions,
                        s.QuestionsAnswered,
                        s.AverageScore,
                        Answers = s.Answers.Select(a => new
                        {
                            a.Question,
                            a.Score,
                            a.AnsweredAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { history = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching interview history");
                return BadRequest(new { error = "Failed to fetch history" });
            }
        }

        [HttpGet("session/{sessionId}")]
        public IActionResult GetSessionStatus(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Ok(new
                {
                    active = true,
                    topic = session.Topic,
                    currentIndex = session.CurrentIndex,
                    totalQuestions = session.Questions.Count,
                    progress = session.Questions.Count > 0 
                        ? (int)((double)session.CurrentIndex / session.Questions.Count * 100) 
                        : 0
                });
            }

            return Ok(new { active = false });
        }

        [HttpPost("skip")]
        public async Task<IActionResult> SkipQuestion([FromForm] int questionIndex, [FromForm] string sessionId)
        {
            try
            {
                _logger.LogInformation("Skipping question for session {SessionId}, question {QuestionIndex}", 
                    sessionId, questionIndex);

                // Get session from memory
                if (!_activeSessions.TryGetValue(sessionId, out var session))
                {
                    return BadRequest(new { message = "Session not found" });
                }

                // Move to next question
                if (questionIndex + 1 < session.Questions.Count)
                {
                    var nextQuestion = session.Questions[questionIndex + 1].Question;
                    
                    _logger.LogInformation("Moving to next question {NextIndex} for session {SessionId}", 
                        questionIndex + 1, sessionId);
                    
                    return Ok(new
                    {
                        message = "Skipped to next question.",
                        nextQuestion = nextQuestion,
                        questionIndex = questionIndex + 1,
                        totalQuestions = session.Questions.Count,
                        isComplete = false
                    });
                }
                else
                {
                    // Interview complete
                    _logger.LogInformation("Interview completed after skip for session {SessionId}", sessionId);
                    _activeSessions.Remove(sessionId);
                    
                    return Ok(new
                    {
                        message = "Interview complete! Great job practicing!",
                        isComplete = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error skipping question");
                return BadRequest(new { error = "Failed to skip question" });
            }
        }

        [HttpGet("debug")]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var allQuestions = await _context.InterviewQuestions.ToListAsync();
                var topics = allQuestions.Select(q => q.Topic).Distinct().ToList();
                var counts = new Dictionary<string, int>();
                
                foreach (var topic in topics)
                {
                    counts[topic] = allQuestions.Count(q => q.Topic == topic);
                }
                
                return Ok(new
                {
                    message = "Interview Controller Debug Info",
                    totalQuestions = allQuestions.Count,
                    topics = topics,
                    counts = counts,
                    sampleQuestions = allQuestions.Take(5).Select(q => new { q.Topic, q.Question })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }
    }

    public class StartInterviewRequest
    {
        public string Topic { get; set; } = string.Empty;
    }

    public class CancelInterviewRequest
    {
        public string? SessionId { get; set; }
    }

    public class InterviewSessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int SessionDbId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public List<InterviewQuestion> Questions { get; set; } = new();
        public int CurrentIndex { get; set; }
        public DateTime StartTime { get; set; }
    }
}