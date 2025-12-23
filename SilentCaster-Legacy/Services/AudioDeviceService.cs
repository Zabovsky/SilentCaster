using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using System.Text.Json;
using System.IO;

namespace SilentCaster.Services
{
    public class AudioDeviceService
    {
        private readonly string _configPath;
        private AudioDeviceConfig _config;

        public AudioDeviceService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_device_config.json");
            LoadConfig();
        }

        public class AudioDeviceConfig
        {
            public string SelectedDeviceId { get; set; } = string.Empty;
            public bool UseCustomDevice { get; set; } = false;
        }

        public class AudioDeviceInfo
        {
            public string DeviceId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Channels { get; set; } = 0;
            public int SampleRate { get; set; } = 0;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AudioDeviceConfig>(json) ?? new AudioDeviceConfig();
                }
                else
                {
                    _config = new AudioDeviceConfig();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки конфигурации аудио устройств: {ex.Message}");
                _config = new AudioDeviceConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения конфигурации аудио устройств: {ex.Message}");
            }
        }

        public List<AudioDeviceInfo> GetAvailableDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            try
            {
                // Получаем все устройства вывода
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);
                    devices.Add(new AudioDeviceInfo
                    {
                        DeviceId = i.ToString(),
                        Name = capabilities.ProductName,
                        Channels = capabilities.Channels,
                        SampleRate = 44100 // Стандартная частота дискретизации
                    });
                }

                // Добавляем устройство по умолчанию
                devices.Insert(0, new AudioDeviceInfo
                {
                    DeviceId = "default",
                    Name = "Устройство по умолчанию (Система)",
                    Channels = 2,
                    SampleRate = 44100
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения списка аудио устройств: {ex.Message}");
                
                // Возвращаем хотя бы устройство по умолчанию
                devices.Add(new AudioDeviceInfo
                {
                    DeviceId = "default",
                    Name = "Устройство по умолчанию (Система)",
                    Channels = 2,
                    SampleRate = 44100
                });
            }

            return devices;
        }

        public AudioDeviceInfo? GetSelectedDevice()
        {
            if (string.IsNullOrEmpty(_config.SelectedDeviceId))
                return GetAvailableDevices().FirstOrDefault();

            var devices = GetAvailableDevices();
            return devices.FirstOrDefault(d => d.DeviceId == _config.SelectedDeviceId) ?? devices.FirstOrDefault();
        }

        public void SetSelectedDevice(string deviceId)
        {
            _config.SelectedDeviceId = deviceId;
            _config.UseCustomDevice = deviceId != "default";
            SaveConfig();
        }

        public AudioDeviceConfig GetConfig() => _config;

        public void UpdateConfig(AudioDeviceConfig config)
        {
            _config = config;
            SaveConfig();
        }

        public bool TestDevice(string deviceId)
        {
            try
            {
                if (deviceId == "default")
                    return true; // Устройство по умолчанию всегда доступно

                var deviceIndex = int.Parse(deviceId);
                if (deviceIndex >= 0 && deviceIndex < WaveOut.DeviceCount)
                {
                    var capabilities = WaveOut.GetCapabilities(deviceIndex);
                    return !string.IsNullOrEmpty(capabilities.ProductName);
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка тестирования аудио устройства: {ex.Message}");
                return false;
            }
        }

        public string GetDeviceDisplayName(string deviceId)
        {
            if (deviceId == "default")
                return "Устройство по умолчанию (Система)";

            try
            {
                var deviceIndex = int.Parse(deviceId);
                if (deviceIndex >= 0 && deviceIndex < WaveOut.DeviceCount)
                {
                    var capabilities = WaveOut.GetCapabilities(deviceIndex);
                    return capabilities.ProductName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения имени устройства: {ex.Message}");
            }

            return "Неизвестное устройство";
        }
    }
} 