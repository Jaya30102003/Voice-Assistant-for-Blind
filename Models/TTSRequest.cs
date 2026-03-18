namespace NerApi.Models
{
    public sealed class TtsRequest
    {
        public string Text { get; set; } = "";
        public int? Volume { get; set; }     // 0..100 (not used by browser TTS)
        public int? Rate   { get; set; }     // -10..10 (not used by browser TTS)
        public string? VoiceName { get; set; }
    }
}