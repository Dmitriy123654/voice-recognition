using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using Vosk;

namespace VoskDemo.Core.Models
{
    public class MicrophoneDevice
    {
        public MMDevice Device { get; }
        public WaveInEvent WaveIn { get; }
        public string Name { get; }
        public int Id { get; }
        public DateTime LastReset { get; set; }
        public VoskRecognizer? Recognizer { get; set; }

        public MicrophoneDevice(MMDevice device, WaveInEvent waveIn, string name, int id)
        {
            Device = device;
            WaveIn = waveIn;
            Name = name;
            Id = id;
            LastReset = DateTime.Now;
        }
    }
} 