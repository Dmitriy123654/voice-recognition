public interface IMicrophoneService
{
    bool IsInitialized { get; }
    string? CurrentMicrophoneName { get; }
    bool IsMultiMode { get; }
    
    void SelectMicrophones(bool multiMode = false);
    void StartRecording();
    void StopRecording();
    void ConfigureMicrophone();
    void Cleanup();
    IEnumerable<string> GetMicrophoneNames();
    void SetDataCallback(Action<byte[], int, string, int> callback);
} 