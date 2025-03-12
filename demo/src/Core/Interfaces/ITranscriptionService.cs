public interface ITranscriptionService
{
    void StartNewSession(string deviceName);
    void AppendTranscription(string deviceName, string text, DateTime timestamp, int? deviceId = null);
    void SaveToFile(string text, string deviceName, bool isMultiMode);
} 