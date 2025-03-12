using NAudio.Wave;
using NAudio.CoreAudioApi;
using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class MicrophoneManager
{
    private List<(MMDevice Device, WaveInEvent WaveIn, string Name, int Id)> _devices = new();
    private Action<byte[], int, string, int> _dataAvailableCallback;
    private bool _isMultiMode = false;

    public bool IsInitialized => _devices.Any();
    public string CurrentMicrophoneName => _devices.FirstOrDefault().Name;
    public bool IsMultiMode => _isMultiMode;

    public void SelectMicrophones(bool multiMode = false)
    {
        _isMultiMode = multiMode;
        try 
        {
            Cleanup();
            
            var enumerator = new MMDeviceEnumerator();
            var availableDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .OrderBy(d => d.FriendlyName)
                .ToList();

            Console.WriteLine("\nДоступные микрофоны:");
            for (int i = 0; i < availableDevices.Count; i++)
            {
                var device = availableDevices[i];
                Console.WriteLine($"{i + 1}. {device.FriendlyName}");
            }

            if (multiMode)
            {
                Console.WriteLine("\nВыберите номера микрофонов через запятую (например: 1,2,3):");
                var input = Console.ReadLine();
                var selectedIndexes = input.Split(',')
                    .Select(x => int.TryParse(x.Trim(), out int num) ? num - 1 : -1)
                    .Where(x => x >= 0 && x < availableDevices.Count)
                    .Distinct()
                    .ToList();

                if (selectedIndexes.Any())
                {
                    foreach (var index in selectedIndexes)
                    {
                        InitializeDevice(index, availableDevices[index], index + 1);
                    }
                    Console.WriteLine($"\nВыбрано микрофонов: {selectedIndexes.Count}");
                    foreach (var device in _devices)
                    {
                        Console.WriteLine($"- {device.Name} (ID: {device.Id})");
                    }
                }
                else
                {
                    Console.WriteLine("Неверный выбор микрофонов");
                }
            }
            else
            {
                Console.Write("\nВыберите номер микрофона: ");
                if (int.TryParse(Console.ReadLine(), out int deviceNumber) && 
                    deviceNumber > 0 && 
                    deviceNumber <= availableDevices.Count)
                {
                    InitializeDevice(deviceNumber - 1, availableDevices[deviceNumber - 1], deviceNumber);
                    Console.WriteLine($"Выбран микрофон: {availableDevices[deviceNumber - 1].FriendlyName}");
                }
                else
                {
                    Console.WriteLine("Неверный выбор микрофона");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении списка микрофонов: {ex.Message}");
        }
    }

    private void InitializeDevice(int deviceNumber, MMDevice device, int id)
    {
        try
        {
            // Получаем все устройства ввода
            var waveInDevices = new List<WaveInCapabilities>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                waveInDevices.Add(WaveInEvent.GetCapabilities(i));
            }

            // Ищем соответствующее устройство по имени
            int waveInDeviceNumber = -1;
            for (int i = 0; i < waveInDevices.Count; i++)
            {
                if (waveInDevices[i].ProductName.Contains(device.FriendlyName) || 
                    device.FriendlyName.Contains(waveInDevices[i].ProductName))
                {
                    waveInDeviceNumber = i;
                    break;
                }
            }

            if (waveInDeviceNumber == -1)
            {
                Console.WriteLine($"Предупреждение: Не удалось найти точное соответствие для {device.FriendlyName}");
                waveInDeviceNumber = deviceNumber;
            }

            var waveIn = new WaveInEvent
            {
                DeviceNumber = waveInDeviceNumber,
                WaveFormat = new WaveFormat(16000, 1)
            };

            string deviceName = device.FriendlyName;
            Console.WriteLine($"Инициализация: {deviceName}");
            
            waveIn.DataAvailable += (s, a) => _dataAvailableCallback?.Invoke(a.Buffer, a.BytesRecorded, deviceName, id);

            _devices.Add((device, waveIn, deviceName, id));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при инициализации устройства {device.FriendlyName}: {ex.Message}");
        }
    }

    public void StartRecording()
    {
        foreach (var device in _devices)
        {
            device.WaveIn.StartRecording();
        }
    }

    public void StopRecording()
    {
        foreach (var device in _devices)
        {
            device.WaveIn.StopRecording();
        }
    }

    public void SetDataCallback(Action<byte[], int, string, int> callback)
    {
        _dataAvailableCallback = callback;
    }

    public void ConfigureMicrophone()
    {
        if (!_devices.Any())
        {
            Console.WriteLine("Сначала выберите микрофон(ы)");
            return;
        }

        bool configuring = true;
        while (configuring)
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
            Console.Write("\nВыберите действие: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    SetVolume();
                    break;
                case "2":
                    ToggleMute();
                    break;
                case "9":
                    configuring = false;
                    break;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }

            if (configuring)
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
    }

    private void SetVolume()
    {
        if (_devices.Count > 1)
        {
            Console.Write($"Выберите номер микрофона (1-{_devices.Count}): ");
            if (!int.TryParse(Console.ReadLine(), out int deviceIndex) || 
                deviceIndex < 1 || 
                deviceIndex > _devices.Count)
            {
                Console.WriteLine("Неверный номер микрофона");
                return;
            }
            deviceIndex--;

            Console.Write("Введите новую громкость (0-100): ");
            if (int.TryParse(Console.ReadLine(), out int volume) && volume >= 0 && volume <= 100)
            {
                _devices[deviceIndex].Device.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100f;
                Console.WriteLine($"Громкость микрофона {_devices[deviceIndex].Name} установлена на {volume}%");
            }
        }
        else
        {
            Console.Write("Введите новую громкость (0-100): ");
            if (int.TryParse(Console.ReadLine(), out int volume) && volume >= 0 && volume <= 100)
            {
                _devices[0].Device.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100f;
                Console.WriteLine($"Громкость установлена на {volume}%");
            }
        }
    }

    private void ToggleMute()
    {
        if (_devices.Count > 1)
        {
            Console.Write($"Выберите номер микрофона (1-{_devices.Count}): ");
            if (!int.TryParse(Console.ReadLine(), out int deviceIndex) || 
                deviceIndex < 1 || 
                deviceIndex > _devices.Count)
            {
                Console.WriteLine("Неверный номер микрофона");
                return;
            }
            deviceIndex--;

            var device = _devices[deviceIndex];
            device.Device.AudioEndpointVolume.Mute = !device.Device.AudioEndpointVolume.Mute;
            Console.WriteLine($"Микрофон {device.Name} {(device.Device.AudioEndpointVolume.Mute ? "выключен" : "включен")}");
        }
        else
        {
            var device = _devices[0];
            device.Device.AudioEndpointVolume.Mute = !device.Device.AudioEndpointVolume.Mute;
            Console.WriteLine($"Микрофон {(device.Device.AudioEndpointVolume.Mute ? "выключен" : "включен")}");
        }
    }

    public void Cleanup()
    {
        foreach (var device in _devices)
        {
            device.WaveIn.Dispose();
        }
        _devices.Clear();
        _dataAvailableCallback = null;
    }

    public IEnumerable<string> GetMicrophoneNames()
    {
        return _devices.Select(d => d.Name);
    }
} 