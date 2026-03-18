using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Services
{
    public interface ILLMService
    {
        Task<InterviewFeedback> EvaluateAnswerAsync(string question, string answer, string topic);
        Task<string> GenerateFollowUpQuestionAsync(string previousQuestion, string previousAnswer, string topic);
        Task<List<string>> SuggestImprovementsAsync(string answer, string topic);
    }
}