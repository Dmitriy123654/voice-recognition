using System;
using System.Threading.Tasks;
using VoskDemo.Core.Configuration;
using VoskDemo.Core.Interfaces;
using VoskDemo.Services;
using VoskDemo.UI;

namespace VoskDemo
{
    public class Program
    {
        private readonly IVoskService _voskService;
        private readonly IMicrophoneService _microphoneService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ConsoleMenu _menu;
        private readonly VoskConfiguration _config;

        public Program()
        {
            _config = new VoskConfiguration();
            _voskService = new VoskService();
            _microphoneService = new MicrophoneService();
            _transcriptionService = new TranscriptionService(_config.TranscriptionDirectory);
            _menu = new ConsoleMenu(_voskService, _microphoneService, _transcriptionService, _config);
        }

        public static async Task Main()
        {
            var program = new Program();
            await program._menu.RunAsync();
        }
    }
} 