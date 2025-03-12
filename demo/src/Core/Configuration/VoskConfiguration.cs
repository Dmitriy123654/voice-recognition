using System;
using System.Collections.Generic;
using System.IO;

namespace VoskDemo.Core.Configuration
{
    public class VoskConfiguration
    {
        public Dictionary<string, string> AvailableModels { get; }
        public int ResetInterval { get; }
        public string TranscriptionDirectory { get; }

        public VoskConfiguration()
        {
            AvailableModels = new Dictionary<string, string>
            {
                { "1", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../models", "vosk-model-small-ru-0.22")) },
                { "2", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../models", "vosk-model-ru-0.42")) }
            };
            ResetInterval = 60;
            TranscriptionDirectory = "transcription";
        }
    }
} 