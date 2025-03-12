using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VoskDemo.Core.Models
{
    public class TranscriptionSession
    {
        public DateTime StartTime { get; }
        public string DeviceName { get; }
        public List<string> ActiveMicrophones { get; }

        public TranscriptionSession(string deviceName, List<string>? activeMicrophones = null)
        {
            StartTime = DateTime.Now;
            DeviceName = deviceName;
            ActiveMicrophones = activeMicrophones ?? new List<string>();
        }

        public string GetSessionHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n=== Новая сессия записи ({StartTime:yyyy-MM-dd HH:mm:ss}) ===");
            
            if (ActiveMicrophones.Any())
            {
                sb.AppendLine("Активные микрофоны:");
                for (int i = 0; i < ActiveMicrophones.Count; i++)
                {
                    sb.AppendLine($"[{i + 1}] - {ActiveMicrophones[i]}");
                }
            }
            
            return sb.ToString();
        }
    }
} 