using System;
using System.Threading.Tasks;
using System.Threading;
using Vosk;
using VoskDemo.Core.Configuration;
using VoskDemo.Core.Interfaces;
using VoskDemo.Core.Models;

namespace VoskDemo.UI
{
    public class ConsoleMenu
    {
        private readonly IVoskService _voskService;
        private readonly IMicrophoneService _microphoneService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly VoskConfiguration _config;
        private bool _isRecording;
        private Model? _model;
        private Dictionary<string, (VoskRecognizer Recognizer, DateTime LastReset)> _recognizers = new();

        public ConsoleMenu(
            IVoskService voskService,
            IMicrophoneService microphoneService,
            ITranscriptionService transcriptionService,
            VoskConfiguration config)
        {
            _voskService = voskService;
            _microphoneService = microphoneService;
            _transcriptionService = transcriptionService;
            _config = config;
        }

        public async Task RunAsync()
        {
            bool running = true;
            while (running)
            {
                DisplayMenu();
                running = await HandleMenuChoiceAsync();
            }
        }

        private void DisplayMenu()
        {
            Console.Clear();
            Console.WriteLine("=== Меню ===");
            Console.WriteLine("1. Загрузить модель");
            Console.WriteLine("2. Выбрать один микрофон");
            Console.WriteLine("3. Выбрать несколько микрофонов");
            Console.WriteLine("4. Начать запись");
            Console.WriteLine("5. Приостановить запись");
            Console.WriteLine("6. Настройки микрофона");
            Console.WriteLine("9. Выход");
            Console.Write("\nВыберите действие: ");
        }

        private async Task<bool> HandleMenuChoiceAsync()
        {
            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    await LoadModelAsync();
                    break;
                case "2":
                    SelectMicrophone();
                    break;
                case "3":
                    SelectMicrophone(true);
                    break;
                case "4":
                    await InitiateRecordingAsync();
                    return true;
                case "5":
                    StopRecording();
                    break;
                case "6":
                    ConfigureMicrophone();
                    break;
                case "9":
                    Cleanup();
                    return false;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }

            WaitForKey();
            return true;
        }

        private async Task LoadModelAsync()
        {
            try
            {
                Console.WriteLine("\nДоступные модели:");
                Console.WriteLine("1. Маленькая модель (быстрее, менее точная)");
                Console.WriteLine("2. Большая модель (медленнее, более точная)");
                Console.Write("\nВыберите модель (1-2): ");

                string choice = Console.ReadLine();
                if (!_config.AvailableModels.ContainsKey(choice))
                {
                    Console.WriteLine("Неверный выбор модели. Используется модель по умолчанию (1)");
                    choice = "1";
                }

                string modelPath = _config.AvailableModels[choice];
                Console.WriteLine($"\nЗагрузка модели из: {modelPath}");
                
                if (!Directory.Exists(modelPath))
                {
                    throw new DirectoryNotFoundException($"Папка модели не найдена: {modelPath}");
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _model = await _voskService.GetModelAsync(modelPath);
                stopwatch.Stop();

                DisplayLoadTime(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка при загрузке модели: {ex.Message}");
            }
        }

        private void DisplayLoadTime(TimeSpan loadTime)
        {
            if (loadTime.TotalMinutes >= 1)
            {
                Console.WriteLine($"Модель успешно загружена! Время загрузки: {(int)loadTime.TotalMinutes} мин {loadTime.Seconds} сек");
            }
            else
            {
                Console.WriteLine($"Модель успешно загружена! Время загрузки: {loadTime.TotalSeconds:F1} сек");
            }
        }

        private void SelectMicrophone(bool multiMode = false)
        {
            _microphoneService.SelectMicrophones(multiMode);
            if (_microphoneService.IsInitialized)
            {
                SetupMicrophoneCallback();
            }
        }

        private void SetupMicrophoneCallback()
        {
            _microphoneService.SetDataCallback((buffer, bytesRecorded, deviceName, deviceId) =>
            {
                ProcessAudioData(buffer, bytesRecorded, deviceName, deviceId);
            });
        }

        private void ProcessAudioData(byte[] buffer, int bytesRecorded, string deviceName, int deviceId)
        {
            if (!_recognizers.ContainsKey(deviceName) || 
                (DateTime.Now - _recognizers[deviceName].LastReset).TotalSeconds > _config.ResetInterval)
            {
                ResetRecognizer(deviceName);
            }

            var (recognizer, _) = _recognizers[deviceName];
            if (recognizer.AcceptWaveform(buffer, bytesRecorded))
            {
                ProcessRecognitionResult(recognizer.Result(), deviceName, deviceId);
            }
        }

        private void ResetRecognizer(string deviceName)
        {
            if (_recognizers.ContainsKey(deviceName))
            {
                _recognizers[deviceName].Recognizer.Dispose();
            }

            var recognizer = new VoskRecognizer(_model, 16000.0f);
            recognizer.SetMaxAlternatives(0);
            recognizer.SetWords(true);
            _recognizers[deviceName] = (recognizer, DateTime.Now);

            _transcriptionService.StartNewSession(deviceName);
        }

        private void ProcessRecognitionResult(string result, string deviceName, int deviceId)
        {
            var text = System.Text.Json.JsonDocument
                .Parse(result)
                .RootElement
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrEmpty(text))
            {
                var timestamp = DateTime.Now;
                _transcriptionService.AppendTranscription(deviceName, text, timestamp, deviceId);
                
                var formattedText = _microphoneService.IsMultiMode ? 
                    $"[{timestamp:HH:mm:ss}] [{deviceId}]: {text}" :
                    $"[{timestamp:HH:mm:ss}]: {text}";
                
                Console.WriteLine(formattedText);
            }
        }

        private async Task InitiateRecordingAsync()
        {
            if (_model == null)
            {
                Console.WriteLine("Модель не загружена! Сначала загрузите модель (пункт 1)");
                WaitForKey();
                await LoadModelAsync();
            }

            if (!_microphoneService.IsInitialized)
            {
                Console.WriteLine("Микрофон не выбран! Выберите микрофон для записи.");
                WaitForKey();
                SelectMicrophone();
            }

            if (_model != null && _microphoneService.IsInitialized)
            {
                await StartRecordingAsync();
            }
        }

        private async Task StartRecordingAsync()
        {
            _isRecording = true;
            _microphoneService.StartRecording();
            
            DisplayRecordingStatus();

            while (_isRecording)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == '5')
                    {
                        StopRecording();
                        break;
                    }
                }
                await Task.Delay(100); // Небольшая задержка чтобы не нагружать процессор
            }
        }

        private void DisplayRecordingStatus()
        {
            if (_microphoneService.IsMultiMode)
            {
                Console.WriteLine("Запись начата. Используются микрофоны:");
                foreach (var name in _microphoneService.GetMicrophoneNames())
                {
                    Console.WriteLine($"- {name}");
                }
            }
            else
            {
                Console.WriteLine($"Запись начата. Используется микрофон: {_microphoneService.CurrentMicrophoneName}");
            }
            Console.WriteLine("Нажмите 5 для остановки записи...");
        }

        private void StopRecording()
        {
            if (!_isRecording)
            {
                Console.WriteLine("Ошибка: Запись не была начата!");
                return;
            }

            if (_model == null)
            {
                Console.WriteLine("Ошибка: Модель не загружена!");
                return;
            }

            if (!_microphoneService.IsInitialized)
            {
                Console.WriteLine("Ошибка: Микрофон не инициализирован!");
                return;
            }

            _microphoneService.StopRecording();
            _isRecording = false;
            Console.WriteLine($"Запись с микрофона {_microphoneService.CurrentMicrophoneName} приостановлена");
        }

        private void ConfigureMicrophone()
        {
            _microphoneService.ConfigureMicrophone();
        }

        private void Cleanup()
        {
            _isRecording = false;
            _microphoneService.Cleanup();
            foreach (var (recognizer, _) in _recognizers.Values)
            {
                recognizer.Dispose();
            }
            _recognizers.Clear();
            _voskService.Cleanup();
        }

        private void WaitForKey()
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }
} 