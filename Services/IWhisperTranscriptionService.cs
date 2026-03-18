using System.IO;
using System.Threading.Tasks;

namespace NerApi.Services
{
    public interface IWhisperTranscriptionService
    {
        Task<string> TranscribeAsync(Stream audioStream);
    }
}