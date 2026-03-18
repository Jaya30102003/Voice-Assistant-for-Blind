using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;
using System.Text.Json;

namespace VoiceAssistantForBlind.Services
{
    public class InterviewService
    {
        private readonly AppDbContext _context;
        private readonly ILLMService _llmService;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(
            AppDbContext context,
            ILLMService llmService,
            ILogger<InterviewService> logger)
        {
            _context = context;
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<List<InterviewQuestion>> GetQuestionsForTopicAsync(string topic)
        {
            // Log the incoming topic for debugging
            _logger.LogInformation("========== GET QUESTIONS CALLED ==========");
            _logger.LogInformation("Original topic received: '{OriginalTopic}'", topic);

            // Normalize the input topic for searching
            var searchTopic = NormalizeTopicForSearch(topic);
            _logger.LogInformation("Searching with normalized topic: '{SearchTopic}'", searchTopic);

            // Get ALL questions from database first to see what we're working with
            var allQuestions = await _context.InterviewQuestions.ToListAsync();
            _logger.LogInformation("Total questions in database: {TotalCount}", allQuestions.Count);

            // Log all distinct topics in the database
            var dbTopics = allQuestions.Select(q => q.Topic).Distinct().ToList();
            _logger.LogInformation("Topics in database: {DbTopics}", string.Join(", ", dbTopics));

            // Find questions where the database topic matches our search topic (case-insensitive)
            var questions = allQuestions
                .Where(q => q.Topic != null && q.Topic.Trim().Equals(searchTopic, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.DisplayOrder)
                .ToList();

            _logger.LogInformation("Found {QuestionCount} questions for normalized topic '{SearchTopic}'", questions.Count, searchTopic);

            if (!questions.Any())
            {
                _logger.LogWarning("No questions found. Input topic: '{OriginalTopic}', Normalized search: '{SearchTopic}'", topic, searchTopic);
                return new List<InterviewQuestion>();
            }

            return questions;
        }

        public async Task<InterviewFeedback> EvaluateAnswerAsync(InterviewQuestion question, string userAnswer, int userId)
        {
            try
            {
                _logger.LogInformation("Evaluating answer for question ID {QuestionId}", question.Id);

                // Get evaluation from LLM
                var feedback = await _llmService.EvaluateAnswerAsync(question.Question, userAnswer, question.Topic);

                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating answer for question {QuestionId}", question.Id);
                return new InterviewFeedback
                {
                    Score = 5,
                    Strengths = new List<string> { "Answer provided" },
                    Improvements = new List<string> { "Unable to evaluate due to technical issue" },
                    Summary = "Answer received but evaluation failed."
                };
            }
        }

        public async Task<InterviewSession> CreateSessionAsync(int userId, string topic, List<InterviewQuestion> questions)
        {
            var session = new InterviewSession
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                Topic = topic,
                StartTime = DateTime.UtcNow,
                TotalQuestions = questions.Count,
                QuestionsAnswered = 0,
                AverageScore = 0,
                Answers = new List<InterviewAnswer>()
            };

            _context.InterviewSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created interview session {SessionId} for user {UserId} with {Count} questions",
                session.SessionId, userId, questions.Count);

            return session;
        }

        public async Task SaveAnswerAsync(
            int sessionId,
            string question,
            string userAnswer,
            InterviewFeedback feedback)
        {
            try
            {
                var answer = new InterviewAnswer
                {
                    SessionId = sessionId,
                    Question = question,
                    UserAnswer = userAnswer,
                    Score = feedback.Score,
                    Feedback = feedback.Summary,
                    Strengths = JsonSerializer.Serialize(feedback.Strengths ?? new List<string>()),
                    Improvements = JsonSerializer.Serialize(feedback.Improvements ?? new List<string>()),
                    AnsweredAt = DateTime.UtcNow
                };

                _context.InterviewAnswers.Add(answer);

                // Update session stats
                var session = await _context.InterviewSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.QuestionsAnswered++;

                    // Recalculate average score
                    var allScores = await _context.InterviewAnswers
                        .Where(a => a.SessionId == sessionId)
                        .Select(a => a.Score)
                        .ToListAsync();

                    if (allScores.Any())
                    {
                        session.AverageScore = allScores.Average();
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved answer for session {SessionId}, question: {Question}",
                    sessionId, question.Substring(0, Math.Min(30, question.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving answer for session {SessionId}", sessionId);
            }
        }

        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return await _context.InterviewQuestions
                .Select(q => q.Topic)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<int> GetQuestionCountForTopicAsync(string topic)
        {
            var searchTopic = NormalizeTopicForSearch(topic);
            var allQuestions = await _context.InterviewQuestions.ToListAsync();
            return allQuestions.Count(q => q.Topic != null && q.Topic.Trim().Equals(searchTopic, StringComparison.OrdinalIgnoreCase));
        }

        // NEW: Dedicated method to normalize a topic for database searching
        private string NormalizeTopicForSearch(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic)) return "general";

            // First, trim and convert to lower case for consistent comparison
            var trimmedLower = topic.Trim().ToLowerInvariant();

            // Handle common variations and map them to the canonical form stored in the database
            return trimmedLower switch
            {
                "c#" => "c#",
                "csharp" => "c#",
                "c sharp" => "c#",
                "see sharp" => "c#",
                "python" => "python",
                "sql" => "sql",
                "java" => "java",
                "javascript" => "javascript",
                "js" => "javascript",
                _ => trimmedLower // Fallback to the trimmed lower-case version
            };
        }
    }
}