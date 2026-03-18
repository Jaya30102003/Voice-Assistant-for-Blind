using Mscc.GenerativeAI;
using VoiceAssistantForBlind.Models;
using System.Text.Json;

namespace VoiceAssistantForBlind.Services
{
    public class GeminiLLMService : ILLMService
    {
        private readonly GenerativeModel _model;
        private readonly ILogger<GeminiLLMService> _logger;
        private readonly HttpClient _httpClient;

        public GeminiLLMService(IConfiguration configuration, ILogger<GeminiLLMService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout
            
            var apiKey = configuration["Gemini:ApiKey"] ?? 
                throw new InvalidOperationException("Gemini API key not configured");
            
            var googleAI = new GoogleAI(apiKey: apiKey);
            
            try
            {
                // Use Flash model for speed
                _model = googleAI.GenerativeModel(model: "gemini-2.0-flash-001");
                _logger.LogInformation("Using Gemini 2.0 Flash model");
            }
            catch
            {
                _model = googleAI.GenerativeModel(model: "gemini-pro");
                _logger.LogInformation("Using Gemini Pro model");
            }
        }

        public async Task<InterviewFeedback> EvaluateAnswerAsync(string question, string answer, string topic)
        {
            try
            {
                _logger.LogInformation("Evaluating answer for topic: {Topic}", topic);

                // FAST PATH: Quick check for "I don't know"
                if (IsIDontKnow(answer))
                {
                    return new InterviewFeedback
                    {
                        Score = 2,
                        Strengths = new List<string> { "Honest response" },
                        Improvements = new List<string> { 
                            "Study fundamentals", 
                            "Attempt partial answers" 
                        },
                        Summary = "Candidate didn't know the answer."
                    };
                }

                // Use timeout token to cancel long-running requests
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                
                // Ultra-concise prompt for speed
                string prompt = $"Score 1-10 this {topic} answer. Q:{question} A:{answer}. Return JSON: score,strengths[],improvements[],summary";

                var response = await _model.GenerateContent(prompt, cancellationToken: cts.Token);
                
                if (response?.Candidates == null || response.Candidates.Count == 0)
                {
                    return GetFastFallback(answer);
                }

                var responseText = response.Candidates[0].Content?.Parts?[0]?.Text;
                
                if (string.IsNullOrEmpty(responseText))
                {
                    return GetFastFallback(answer);
                }

                // Quick JSON extraction
                var jsonStart = responseText.IndexOf('{');
                var jsonEnd = responseText.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = responseText.Substring(jsonStart, jsonEnd - jsonStart);
                    var feedback = JsonSerializer.Deserialize<InterviewFeedback>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (feedback != null)
                    {
                        feedback.Score = Math.Clamp(feedback.Score, 1, 10);
                        return feedback;
                    }
                }

                return GetFastFallback(answer);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Gemini API timed out for topic {Topic}", topic);
                return GetFastFallback(answer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API for topic {Topic}", topic);
                return GetFastFallback(answer);
            }
        }

        private bool IsIDontKnow(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return true;
            
            var lower = answer.ToLower();
            return lower.Contains("don't know") || 
                   lower.Contains("dont know") ||
                   lower.Contains("not sure") ||
                   lower.Contains("no idea") ||
                   answer.Length < 15;
        }

        private InterviewFeedback GetFastFallback(string answer)
        {
            // Ultra-fast fallback based on answer length
            if (IsIDontKnow(answer))
            {
                return new InterviewFeedback
                {
                    Score = 2,
                    Strengths = new List<string> { "Attempted to respond" },
                    Improvements = new List<string> { "Study more", "Be more specific" },
                    Summary = "Answer was insufficient."
                };
            }
            
            if (answer.Length < 50)
            {
                return new InterviewFeedback
                {
                    Score = 4,
                    Strengths = new List<string> { "Brief answer provided" },
                    Improvements = new List<string> { "Add more detail", "Include examples" },
                    Summary = "Answer was too brief."
                };
            }
            
            return new InterviewFeedback
            {
                Score = 6,
                Strengths = new List<string> { "Answered the question" },
                Improvements = new List<string> { "Add technical depth", "Structure better" },
                Summary = "Average answer."
            };
        }

        public async Task<string> GenerateFollowUpQuestionAsync(string previousQuestion, string previousAnswer, string topic)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                string prompt = $"Follow-up {topic} question. Previous:{previousQuestion}. Answer:{previousAnswer}. Return ONLY question.";
                
                var response = await _model.GenerateContent(prompt, cancellationToken: cts.Token);
                return response?.Candidates?[0]?.Content?.Parts?[0]?.Text?.Trim() ?? 
                    "Can you elaborate?";
            }
            catch
            {
                return "Can you tell me more?";
            }
        }

        public async Task<List<string>> SuggestImprovementsAsync(string answer, string topic)
        {
            return new List<string> 
            { 
                "Study core concepts", 
                "Practice with examples", 
                "Use technical terms" 
            };
        }
    }
}
