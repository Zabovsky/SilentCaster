using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster.Services
{
    public class SpeechService
    {
        private readonly SpeechSynthesizer _synthesizer;
        private readonly ExternalTTSService _externalTTS;
        private readonly AudioDeviceService? _audioDeviceService;
        private VoiceSettings _settings;
        private readonly Random _random;

        public SpeechService(AudioDeviceService? audioDeviceService = null)
        {
            _synthesizer = new SpeechSynthesizer();
            _externalTTS = new ExternalTTSService();
            _audioDeviceService = audioDeviceService;
            _settings = new VoiceSettings();
            _random = new Random();
            LoadDefaultVoice();
            InitializeDefaultVoiceProfiles();
        }

        public List<string> GetAvailableVoices()
        {
            var voices = _synthesizer.GetInstalledVoices()
                .Where(v => v.Enabled)
                .Select(v => v.VoiceInfo.Name)
                .ToList();
            
            // Добавляем внешние голоса
            if (_externalTTS.GetConfig().UseExternalTTS)
            {
                voices.AddRange(_externalTTS.GetAvailableVoices());
            }
            
            return voices;
        }

        public void UpdateSettings(VoiceSettings settings)
        {
            _settings = settings;
            ApplySettings();
        }

        public async Task SpeakAsync(string text, string? username = null, string messageType = "chat")
        {
            if (string.IsNullOrEmpty(text)) return;

            // Заменяем плейсхолдеры
            var processedText = text.Replace("{username}", username ?? "пользователь");

            await Task.Run(async () =>
            {
                try
                {
                    if (_settings.UseMultipleVoices && _settings.VoiceProfiles.Any(p => p.IsEnabled))
                    {
                        // Фильтруем профили по типу сообщения и настройкам взаимодействия
                        var enabledProfiles = _settings.VoiceProfiles.Where(p => p.IsEnabled && 
                            (messageType == "chat" && p.UseForChatMessages ||
                             messageType == "quick" && p.UseForQuickResponses ||
                             messageType == "manual" && p.UseForManualMessages)).ToList();
                        
                        if (enabledProfiles.Any())
                        {
                            // Применяем приоритеты и шансы использования
                            var weightedProfiles = new List<VoiceProfile>();
                            foreach (var profile in enabledProfiles)
                            {
                                var weight = (int)(profile.Priority * profile.UsageChance / 100.0);
                                for (int i = 0; i < weight; i++)
                                {
                                    weightedProfiles.Add(profile);
                                }
                            }
                            
                            if (weightedProfiles.Any())
                            {
                                var selectedProfile = weightedProfiles[_random.Next(weightedProfiles.Count())];
                                await ApplyVoiceProfileAsync(selectedProfile, processedText);
                                return;
                            }
                        }
                    }

                    // Используем основной голос
                    await ApplyVoiceProfileAsync(null, processedText);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но не прерываем работу
                    System.Diagnostics.Debug.WriteLine($"Ошибка синтеза речи: {ex.Message}");
                }
            });
        }

        public async Task SpeakTestAsync()
        {
            await SpeakAsync("Это тестовое сообщение для проверки голоса.");
        }

        public async Task SpeakTestAsync(VoiceProfile profile)
        {
            if (string.IsNullOrEmpty(profile.VoiceName)) return;

            await Task.Run(async () =>
            {
                try
                {
                    await ApplyVoiceProfileAsync(profile, "Тест голоса: " + profile.Name);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка тестирования голоса: {ex.Message}");
                }
            });
        }

        public VoiceProfile CreateVoiceProfile(string name, string voiceName, double rate = 0, double volume = 100, string description = "")
        {
            return new VoiceProfile
            {
                Name = name,
                VoiceName = voiceName,
                Rate = rate,
                Volume = volume,
                Description = description,
                IsEnabled = true,
                UseForChatMessages = true,
                UseForQuickResponses = true,
                UseForManualMessages = true,
                Priority = 1,
                UsageChance = 100.0
            };
        }

        public void AddVoiceProfile(VoiceProfile profile)
        {
            _settings.VoiceProfiles.Add(profile);
        }

        public void RemoveVoiceProfile(int index)
        {
            if (index >= 0 && index < _settings.VoiceProfiles.Count())
            {
                _settings.VoiceProfiles.RemoveAt(index);
            }
        }

        public void UpdateVoiceProfile(int index, VoiceProfile profile)
        {
            if (index >= 0 && index < _settings.VoiceProfiles.Count())
            {
                _settings.VoiceProfiles[index] = profile;
            }
        }

        private void LoadDefaultVoice()
        {
            var voices = GetAvailableVoices();
            if (voices.Any())
            {
                _settings.SelectedVoice = voices.First();
                ApplySettings();
            }
        }

        private void InitializeDefaultVoiceProfiles()
        {
            var voices = GetAvailableVoices();
            if (voices.Count >= 3)
            {
                // Создаем несколько профилей по умолчанию
                _settings.VoiceProfiles.Add(CreateVoiceProfile("Основной", voices[0], 0, 100, "Основной голос для чтения"));
                _settings.VoiceProfiles.Add(CreateVoiceProfile("Быстрый", voices[1 % voices.Count], 2, 90, "Быстрый голос"));
                _settings.VoiceProfiles.Add(CreateVoiceProfile("Медленный", voices[2 % voices.Count], -2, 95, "Медленный и четкий голос"));
            }
            else if (voices.Any())
            {
                _settings.VoiceProfiles.Add(CreateVoiceProfile("Основной", voices[0], 0, 100, "Основной голос"));
            }
        }

        private void ApplySettings()
        {
            if (!string.IsNullOrEmpty(_settings.SelectedVoice))
            {
                try
                {
                    // Проверяем, является ли голос внешним
                    if (_settings.SelectedVoice.Contains(" - "))
                    {
                        // Это внешний голос, не применяем к синтезатору
                        return;
                    }
                    
                    _synthesizer.SelectVoice(_settings.SelectedVoice);
                }
                catch
                {
                    // Если выбранный голос недоступен, используем первый доступный
                    LoadDefaultVoice();
                }
            }

            _synthesizer.Rate = (int)_settings.Rate;
            _synthesizer.Volume = (int)_settings.Volume;
            
            // Применяем выбранное аудио устройство
            ApplyAudioDevice();
        }

        private async Task ApplyVoiceProfileAsync(VoiceProfile? profile, string text)
        {
            try
            {
                if (profile != null)
                {
                    // Проверяем, является ли голос внешним
                    if (profile.VoiceName.Contains(" - "))
                    {
                        // Извлекаем имя голоса из строки "Provider - Voice"
                        var voiceName = profile.VoiceName.Substring(profile.VoiceName.IndexOf(" - ") + 3);
                        var success = await _externalTTS.SpeakAsync(text, voiceName, profile.Rate, profile.Volume);
                        
                        if (!success)
                        {
                            // Если внешний TTS не сработал, используем встроенный
                            _synthesizer.SpeakAsync(text);
                        }
                    }
                    else
                    {
                        // Встроенный голос
                        _synthesizer.SelectVoice(profile.VoiceName);
                        _synthesizer.Rate = (int)profile.Rate;
                        _synthesizer.Volume = (int)profile.Volume;
                        _synthesizer.SpeakAsync(text);
                    }
                }
                else
                {
                    // Используем текущие настройки
                    _synthesizer.SpeakAsync(text);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения профиля голоса: {ex.Message}");
                // Возвращаемся к основным настройкам
                ApplySettings();
                _synthesizer.SpeakAsync(text);
            }
        }

        private void ApplyAudioDevice()
        {
            if (_audioDeviceService == null) return;
            
            try
            {
                var selectedDevice = _audioDeviceService.GetSelectedDevice();
                if (selectedDevice != null && selectedDevice.DeviceId != "default")
                {
                    // К сожалению, System.Speech.Synthesis.SpeechSynthesizer не поддерживает
                    // прямой выбор аудио устройства. Мы можем только логировать информацию
                    System.Diagnostics.Debug.WriteLine($"Выбранное аудио устройство: {selectedDevice.Name} (ID: {selectedDevice.DeviceId})");
                    
                    // Для полной поддержки выбора аудио устройства нужно использовать
                    // альтернативные библиотеки, такие как NAudio с SAPI или другие TTS решения
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения аудио устройства: {ex.Message}");
            }
        }

        public ExternalTTSService GetExternalTTS() => _externalTTS;

        public void Dispose()
        {
            _synthesizer?.Dispose();
        }
    }
} 