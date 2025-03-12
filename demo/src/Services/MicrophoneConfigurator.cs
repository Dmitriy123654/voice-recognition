using System;
using System.Collections.Generic;
using VoskDemo.Core.Models;
using NAudio.CoreAudioApi;
using System.Linq;

namespace VoskDemo.Services
{
    internal class MicrophoneConfigurator
    {
        private readonly List<MicrophoneDevice> _devices;

        public MicrophoneConfigurator(List<MicrophoneDevice> devices)
        {
            _devices = devices;
        }

        public void Configure()
        {
            bool configuring = true;
            while (configuring)
            {
                DisplayCurrentState();
                configuring = HandleUserInput();
            }
        }

        private void DisplayCurrentState()
        {
            Console.Clear();
            Console.WriteLine("=== Настройки микрофонов ===");
            for (int i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                Console.WriteLine($"\nМикрофон {i + 1}: {device.Name}");
                Console.WriteLine($"Громкость: {device.Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100:F0}%");
                Console.WriteLine($"Состояние: {(device.Device.AudioEndpointVolume.Mute ? "Выключен" : "Включен")}");
            }

            Console.WriteLine("\nУправление:");
            Console.WriteLine("1. Изменить громкость");
            Console.WriteLine("2. Включить/выключить микрофон");
            Console.WriteLine("9. Вернуться в главное меню");
        }

        private bool HandleUserInput()
        {
            Console.Write("\nВыберите действие: ");
            return Console.ReadLine() switch
            {
                "1" => HandleVolumeChange(),
                "2" => HandleMuteToggle(),
                "9" => false,
                _ => ShowInvalidChoice()
            };
        }

        private bool HandleVolumeChange()
        {
            var device = SelectDevice("установки громкости");
            if (device == null) return true;

            Console.Write("Введите новую громкость (0-100): ");
            if (int.TryParse(Console.ReadLine(), out int volume) && volume >= 0 && volume <= 100)
            {
                device.Device.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100f;
                Console.WriteLine($"Громкость микрофона {device.Name} установлена на {volume}%");
            }

            WaitForKey();
            return true;
        }

        private bool HandleMuteToggle()
        {
            var device = SelectDevice("переключения состояния");
            if (device == null) return true;

            device.Device.AudioEndpointVolume.Mute = !device.Device.AudioEndpointVolume.Mute;
            Console.WriteLine($"Микрофон {device.Name} {(device.Device.AudioEndpointVolume.Mute ? "выключен" : "включен")}");

            WaitForKey();
            return true;
        }

        private MicrophoneDevice? SelectDevice(string action)
        {
            if (_devices.Count == 1)
                return _devices[0];

            Console.Write($"\nВыберите номер микрофона для {action} (1-{_devices.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int deviceIndex) && 
                deviceIndex >= 1 && 
                deviceIndex <= _devices.Count)
            {
                return _devices[deviceIndex - 1];
            }

            Console.WriteLine("Неверный номер микрофона");
            return null;
        }

        private bool ShowInvalidChoice()
        {
            Console.WriteLine("Неверный выбор");
            WaitForKey();
            return true;
        }

        private void WaitForKey()
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }

        private MMDevice? FindDevice(string name)
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .FirstOrDefault(d => d.FriendlyName.Contains(name));
        }
    }
} 