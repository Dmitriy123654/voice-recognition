# Voice Recognition

Проект для распознавания речи с использованием Vosk.

## Требования

- .NET 6.0 SDK ([скачать](https://dotnet.microsoft.com/download/dotnet/6.0))

## Установка

1. Установите .NET 6.0 SDK если не установлен
2. Скачайте модели Vosk для русского языка(https://alphacephei.com/vosk/models) и разместите их в папке models/:
   - [vosk-model-small-ru-0.22](https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip)
   - [vosk-model-ru-0.42](https://alphacephei.com/vosk/models/vosk-model-ru-0.42.zip)

2. Распакуйте модели в соответствующие папки,должно получиться вот так:
models/
├── vosk-model-small-ru-0.22/
└── vosk-model-ru-0.42/

## Структура проекта
- demo/ - Исходный код проекта
- nuget/ - Пакеты NuGet (предустановлены)
- models/ - Папка для моделей Vosk (требуется скачать модели)

## Запуск

1. Скачайте и распакуйте модели как описано выше
2. Запустите проект:
- cd demo
- dotnet restore
- dotnet run

## Использование

1. Загрузите модель
2. Выберите микрофон(ы)
3. Начните запись
4. Нажмите 5 для остановки записи