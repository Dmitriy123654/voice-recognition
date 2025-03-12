using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;  // Добавляем для ToList()
using Vosk;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections.Generic;
using System.Text;

public class VoskDemo
{
    private static Model _model;
    private static readonly int RESET_INTERVAL = 60; // Сброс каждые 60 секунд
    private static Dictionary<string, (VoskRecognizer Recognizer, DateTime LastReset)> _recognizers = new();
    private static StringBuilder _currentText = new StringBuilder();
    private static bool _isRecording = false;
    private static readonly Dictionary<string, string> AVAILABLE_MODELS = new()
    {
        { "1", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../models", "vosk-model-small-ru-0.22")) },
        { "2", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../models", "vosk-model-ru-0.42")) }
    };
    private static MicrophoneManager _microphone = new MicrophoneManager();

    public static void Main()
    {
        Vosk.Vosk.SetLogLevel(0);
        bool running = true;

        while (running)
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

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    LoadModel();
                    break;
                case "2":
                    SelectMicrophone();
                    break;
                case "3":
                    SelectMicrophone(true);
                    break;
                case "4":
                    InitiateRecording();
                    break;
                case "5":
                    StopRecording();
                    break;
                case "6":
                    ConfigureMicrophone();
                    break;
                case "9":
                    running = false;
                    Cleanup();
                    break;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }

            if (choice != "4") // Если не режим записи
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
    }

    private static async void LoadModel()
    {
        try
        {
            Console.WriteLine("\nДоступные модели:");
            Console.WriteLine("1. Маленькая модель (быстрее, менее точная)");
            Console.WriteLine("2. Большая модель (медленнее, более точная)");
            Console.Write("\nВыберите модель (1-2): ");

            string choice = Console.ReadLine();
            if (!AVAILABLE_MODELS.ContainsKey(choice))
            {
                Console.WriteLine("Неверный выбор модели. Используется модель по умолчанию (1)");
                choice = "1";
            }

            string modelPath = AVAILABLE_MODELS[choice];
            Console.WriteLine($"\nЗагрузка модели из: {modelPath}");
            
            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException($"Папка модели не найдена: {modelPath}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            await Task.Run(() =>
            {
                _model = VoskService.GetModel(modelPath, false);
            });

            stopwatch.Stop();
            TimeSpan loadTime = stopwatch.Elapsed;
            
            if (loadTime.TotalMinutes >= 1)
            {
                Console.WriteLine($"Модель успешно загружена! Время загрузки: {(int)loadTime.TotalMinutes} мин {loadTime.Seconds} сек");
            }
            else
            {
                Console.WriteLine($"Модель успешно загружена! Время загрузки: {loadTime.TotalSeconds:F1} сек");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nОшибка при загрузке модели: {ex.Message}");
        }
    }

    private static void SelectMicrophone(bool multiMode = false)
    {
        // Создаем папку для транскрипций, если её нет
        Directory.CreateDirectory("transcription");
        
        _microphone.SelectMicrophones(multiMode);
        _microphone.SetDataCallback((buffer, bytesRecorded, deviceName, deviceId) =>
        {
            // Проверяем, нужно ли создать или сбросить распознаватель
            if (!_recognizers.ContainsKey(deviceName) || 
                (DateTime.Now - _recognizers[deviceName].LastReset).TotalSeconds > RESET_INTERVAL)
            {
                if (_recognizers.ContainsKey(deviceName))
                {
                    _recognizers[deviceName].Recognizer.Dispose();
                }

                var recognizer = new VoskRecognizer(_model, 16000.0f);
                recognizer.SetMaxAlternatives(0);
                recognizer.SetWords(true);
                _recognizers[deviceName] = (recognizer, DateTime.Now);
            }

            var (currentRecognizer, _) = _recognizers[deviceName];
            if (currentRecognizer.AcceptWaveform(buffer, bytesRecorded))
            {
                var result = currentRecognizer.Result();
                var text = System.Text.Json.JsonDocument
                    .Parse(result)
                    .RootElement
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrEmpty(text))
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    var formattedText = _microphone.IsMultiMode ? 
                        $"[{timestamp}] [{deviceId}]: {text}" :
                        $"[{timestamp}]: {text}";
                    
                    Console.WriteLine(formattedText);
                    
                    // Записываем в индивидуальный файл микрофона
                    File.AppendAllText(Path.Combine("transcription", $"transcript_{deviceName}.txt"), 
                        text + Environment.NewLine);
                    
                    // Если включен мультирежим, записываем также в общий файл
                    if (_microphone.IsMultiMode)
                    {
                        File.AppendAllText(Path.Combine("transcription", "transcript_all.txt"), 
                            formattedText + Environment.NewLine);
                    }
                }
            }
        });
    }

    private static void InitiateRecording()
    {
        if (_model == null)
        {
            Console.WriteLine("Модель не загружена! Сначала загрузите модель (пункт 1)");
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
            LoadModel();
        }

        if (!_microphone.IsInitialized)
        {
            Console.WriteLine("Микрофон не выбран! Выберите микрофон для записи.");
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
            SelectMicrophone();
        }

        if (_model != null && _microphone.IsInitialized)
        {
            StartRecording();
        }
    }

    private static void StartRecording()
    {
        _isRecording = true;
        _microphone.StartRecording();
        
        if (_microphone.IsMultiMode)
        {
            Console.WriteLine("Запись начата. Используются микрофоны:");
            var sessionStart = $"\n=== Новая сессия записи ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===\n";
            sessionStart += "Активные микрофоны:\n";
            
            var micNames = _microphone.GetMicrophoneNames().ToList();
            for (int i = 0; i < micNames.Count; i++)
            {
                var micInfo = $"[{i + 1}] - {micNames[i]}";
                Console.WriteLine(micInfo);
                sessionStart += micInfo + "\n";
            }
            sessionStart += "\n";
            File.AppendAllText(Path.Combine("transcription", "transcript_all.txt"), sessionStart);
        }
        else
        {
            Console.WriteLine($"Запись начата. Используется микрофон: {_microphone.CurrentMicrophoneName}");
        }
        
        Console.WriteLine("Нажмите 5 для остановки записи...");
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
            Thread.Sleep(100); // Небольшая задержка чтобы не нагружать процессор
        }
    }

    private static void StopRecording()
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

        if (!_microphone.IsInitialized)
        {
            Console.WriteLine("Ошибка: Микрофон не инициализирован!");
            return;
        }

        _microphone.StopRecording();
        _isRecording = false;
        Console.WriteLine($"Запись с микрофона {_microphone.CurrentMicrophoneName} приостановлена");
    }

    private static void ConfigureMicrophone()
    {
        _microphone.ConfigureMicrophone();
    }

    private static void Cleanup()
    {
        _isRecording = false;
        _microphone.Cleanup();
        foreach (var (recognizer, _) in _recognizers.Values)
        {
            recognizer.Dispose();
        }
        _recognizers.Clear();
        _model?.Dispose();
   }
}
