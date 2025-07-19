using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace SilentCaster.Services
{
    public class ExternalTTSService
    {
        private readonly string _configPath;
        private ExternalTTSConfig _config;

        public ExternalTTSService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external_tts_config.json");
            LoadConfig();
        }

        public class ExternalTTSConfig
        {
            public List<ExternalTTSProvider> Providers { get; set; } = new List<ExternalTTSProvider>();
            public bool UseExternalTTS { get; set; } = false;
            public string DefaultProvider { get; set; } = "";
        }

        public class ExternalTTSProvider
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = ""; // "ttsvoicewizard", "espeak", "rhvoice", "custom"
            public string ExecutablePath { get; set; } = "";
            public string Arguments { get; set; } = "";
            public bool IsEnabled { get; set; } = true;
            public List<ExternalVoice> Voices { get; set; } = new List<ExternalVoice>();
        }

        public class ExternalVoice
        {
            public string Name { get; set; } = "";
            public string VoiceId { get; set; } = "";
            public string Language { get; set; } = "ru-RU";
            public double Rate { get; set; } = 0;
            public double Volume { get; set; } = 100;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<ExternalTTSConfig>(json) ?? new ExternalTTSConfig();
                }
                else
                {
                    _config = new ExternalTTSConfig();
                    CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки конфигурации TTS: {ex.Message}");
                _config = new ExternalTTSConfig();
                CreateDefaultConfig();
            }
        }

        private void CreateDefaultConfig()
        {
            // Добавляем TTS Voice Wizard по умолчанию
            var ttsWizard = new ExternalTTSProvider
            {
                Name = "TTS Voice Wizard",
                Type = "ttsvoicewizard",
                ExecutablePath = @"C:\Program Files\TTS Voice Wizard\TTSVoiceWizard.exe",
                Arguments = "-voice \"{voice}\" -rate {rate} -volume {volume} -text \"{text}\"",
                IsEnabled = false,
                Voices = new List<ExternalVoice>
                {
                    new ExternalVoice { Name = "Microsoft Anna", VoiceId = "Microsoft Anna", Language = "en-US" },
                    new ExternalVoice { Name = "Microsoft Irina", VoiceId = "Microsoft Irina", Language = "ru-RU" }
                }
            };

            _config.Providers.Add(ttsWizard);
            SaveConfig();
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
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения конфигурации TTS: {ex.Message}");
            }
        }

        public async Task<bool> SpeakAsync(string text, string voiceName, double rate = 0, double volume = 100)
        {
            if (!_config.UseExternalTTS || string.IsNullOrEmpty(_config.DefaultProvider))
                return false;

            var provider = _config.Providers.FirstOrDefault(p => p.Name == _config.DefaultProvider && p.IsEnabled);
            if (provider == null)
                return false;

            try
            {
                var voice = provider.Voices.FirstOrDefault(v => v.Name == voiceName);
                if (voice == null)
                    return false;

                var arguments = provider.Arguments
                    .Replace("{voice}", voice.VoiceId)
                    .Replace("{rate}", rate.ToString())
                    .Replace("{volume}", volume.ToString())
                    .Replace("{text}", $"\"{text}\"");

                var startInfo = new ProcessStartInfo
                {
                    FileName = provider.ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка внешнего TTS: {ex.Message}");
                return false;
            }
        }

        public List<string> GetAvailableVoices()
        {
            var voices = new List<string>();
            
            foreach (var provider in _config.Providers.Where(p => p.IsEnabled))
            {
                voices.AddRange(provider.Voices.Select(v => $"{provider.Name} - {v.Name}"));
            }
            
            return voices;
        }

        public ExternalTTSConfig GetConfig() => _config;
        
        public void UpdateConfig(ExternalTTSConfig config)
        {
            _config = config;
            SaveConfig();
        }

        public bool TestProvider(string providerName)
        {
            var provider = _config.Providers.FirstOrDefault(p => p.Name == providerName);
            if (provider == null) return false;

            return File.Exists(provider.ExecutablePath);
        }

        public void AddProvider(ExternalTTSProvider provider)
        {
            _config.Providers.Add(provider);
            SaveConfig();
        }

        public void RemoveProvider(string providerName)
        {
            var provider = _config.Providers.FirstOrDefault(p => p.Name == providerName);
            if (provider != null)
            {
                _config.Providers.Remove(provider);
                SaveConfig();
            }
        }
    }
} 