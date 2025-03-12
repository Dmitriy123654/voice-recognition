using System;
using System.IO;
using VoskDemo.Core.Models;

namespace VoskDemo.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly string _transcriptionDirectory;

        public TranscriptionService(string transcriptionDirectory)
        {
            _transcriptionDirectory = transcriptionDirectory;
            Directory.CreateDirectory(_transcriptionDirectory);
        }

        public void StartNewSession(string deviceName)
        {
            var session = new TranscriptionSession(deviceName);
            var header = session.GetSessionHeader();
            File.AppendAllText(Path.Combine(_transcriptionDirectory, $"transcript_{deviceName}.txt"), header);
        }

        public void AppendTranscription(string deviceName, string text, DateTime timestamp, int? deviceId = null)
        {
            var formattedText = deviceId.HasValue ? 
                $"[{timestamp:HH:mm:ss}] [{deviceId}]: {text}" :
                $"[{timestamp:HH:mm:ss}]: {text}";

            File.AppendAllText(
                Path.Combine(_transcriptionDirectory, $"transcript_{deviceName}.txt"), 
                text + Environment.NewLine);

            if (deviceId.HasValue)
            {
                File.AppendAllText(
                    Path.Combine(_transcriptionDirectory, "transcript_all.txt"),
                    formattedText + Environment.NewLine);
            }
        }

        public void SaveToFile(string text, string deviceName, bool isMultiMode)
        {
            var filePath = Path.Combine(_transcriptionDirectory, $"transcript_{deviceName}.txt");
            File.AppendAllText(filePath, text + Environment.NewLine);
        }
    }
} 