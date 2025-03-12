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
            // Определяем базовый путь в зависимости от режима запуска
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var modelsPath = "";

            // Проверяем, запущены ли мы из релизной сборки
            if (basePath.Contains("Release") || basePath.Contains("publish"))
            {
                // Для релизной версии модели лежат рядом с exe
                modelsPath = Path.Combine(basePath, "models");
            }
            else
            {
                // Для отладочной версии идем вверх к корню проекта
                modelsPath = Path.GetFullPath(Path.Combine(basePath, "../../../../../release/models"));
            }

            Console.WriteLine($"Путь к exe: {basePath}");
            Console.WriteLine($"Путь к моделям: {modelsPath}");

            AvailableModels = new Dictionary<string, string>
            {
                { "1", Path.Combine(modelsPath, "vosk-model-small-ru-0.22") },
                { "2", Path.Combine(modelsPath, "vosk-model-ru-0.42") }
            };

            // Проверяем пути
            Console.WriteLine($"Путь к маленькой модели: {AvailableModels["1"]}");
            if (Directory.Exists(AvailableModels["1"]))
                Console.WriteLine("Папка маленькой модели найдена");
            else
                Console.WriteLine("Папка маленькой модели НЕ найдена");

            ResetInterval = 60;
            TranscriptionDirectory = "transcription";
        }
    }
} 