using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using VoskDemo.Core.Models;
using VoskDemo.Core.Interfaces;

namespace VoskDemo.Services
{
    public class MicrophoneService : IMicrophoneService
    {
        private List<MicrophoneDevice> _devices = new();
        private Action<byte[], int, string, int> _dataAvailableCallback;
        private bool _isMultiMode = false;

        public bool IsInitialized => _devices.Any();
        public string CurrentMicrophoneName => _devices.FirstOrDefault()?.Name;
        public bool IsMultiMode => _isMultiMode;

        public void SelectMicrophones(bool multiMode = false)
        {
            _isMultiMode = multiMode;
            try 
            {
                Cleanup();
                var devices = GetAvailableDevices();
                DisplayAvailableDevices(devices);

                if (multiMode)
                {
                    HandleMultiModeSelection(devices);
                }
                else
                {
                    HandleSingleModeSelection(devices);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении списка микрофонов: {ex.Message}");
            }
        }

        private List<MMDevice> GetAvailableDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .OrderBy(d => d.FriendlyName)
                .ToList();
        }

        private void DisplayAvailableDevices(List<MMDevice> devices)
        {
            Console.WriteLine("\nДоступные микрофоны:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {devices[i].FriendlyName}");
            }
        }

        private void HandleMultiModeSelection(List<MMDevice> availableDevices)
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
                DisplaySelectedDevices();
            }
            else
            {
                Console.WriteLine("Неверный выбор микрофонов");
            }
        }

        private void HandleSingleModeSelection(List<MMDevice> availableDevices)
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

        private void InitializeDevice(int deviceNumber, MMDevice device, int id)
        {
            try
            {
                var waveInDeviceNumber = FindMatchingWaveInDevice(device, deviceNumber);
                var waveIn = CreateWaveInEvent(waveInDeviceNumber);
                
                var micDevice = new MicrophoneDevice(device, waveIn, device.FriendlyName, id);
                Console.WriteLine($"Инициализация: {micDevice.Name}");
                
                waveIn.DataAvailable += (s, a) => 
                    _dataAvailableCallback?.Invoke(a.Buffer, a.BytesRecorded, micDevice.Name, micDevice.Id);

                _devices.Add(micDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации устройства {device.FriendlyName}: {ex.Message}");
            }
        }

        private int FindMatchingWaveInDevice(MMDevice device, int fallbackDeviceNumber)
        {
            var waveInDevices = Enumerable.Range(0, WaveInEvent.DeviceCount)
                .Select(i => WaveInEvent.GetCapabilities(i))
                .ToList();

            return waveInDevices
                .FindIndex(w => w.ProductName.Contains(device.FriendlyName) || 
                               device.FriendlyName.Contains(w.ProductName));
        }

        private WaveInEvent CreateWaveInEvent(int deviceNumber)
        {
            return new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(16000, 1)
            };
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

        public void ConfigureMicrophone()
        {
            if (!IsInitialized)
            {
                Console.WriteLine("Сначала выберите микрофон(ы)");
                return;
            }

            var configurator = new MicrophoneConfigurator(_devices);
            configurator.Configure();
        }

        public void SetDataCallback(Action<byte[], int, string, int> callback)
        {
            _dataAvailableCallback = callback;
        }

        public IEnumerable<string> GetMicrophoneNames()
        {
            return _devices.Select(d => d.Name);
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

        private void DisplaySelectedDevices()
        {
            Console.WriteLine($"\nВыбрано микрофонов: {_devices.Count}");
            foreach (var device in _devices)
            {
                Console.WriteLine($"- {device.Name}");
            }
        }
    }
} 