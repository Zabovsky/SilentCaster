using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster.Services
{
    public class AdvancedSpeechService
    {
        private readonly SpeechSynthesizer _synthesizer;
        private readonly AudioDeviceService? _audioDeviceService;
        private readonly Random _random;

        public AdvancedSpeechService(AudioDeviceService? audioDeviceService = null)
        {
            _synthesizer = new SpeechSynthesizer();
            _audioDeviceService = audioDeviceService;
            _random = new Random();
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
                    await SpeakWithSelectedDeviceAsync(processedText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка синтеза речи: {ex.Message}");
                }
            });
        }

        public async Task SpeakTestAsync()
        {
            await SpeakAsync("Это тестовое сообщение для проверки голоса.");
        }

        private async Task SpeakWithSelectedDeviceAsync(string text)
        {
            try
            {
                // Создаем временный файл для аудио
                var tempFile = Path.GetTempFileName();
                tempFile = Path.ChangeExtension(tempFile, ".wav");

                // Синтезируем речь в файл
                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    _synthesizer.SetOutputToWaveStream(stream);
                    _synthesizer.Speak(text);
                }

                // Воспроизводим через выбранное устройство
                await PlayAudioFileAsync(tempFile);

                // Удаляем временный файл
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Игнорируем ошибки удаления временного файла
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения через выбранное устройство: {ex.Message}");
                // Возвращаемся к стандартному воспроизведению
                _synthesizer.SetOutputToDefaultAudioDevice();
                _synthesizer.Speak(text);
            }
        }

        private async Task PlayAudioFileAsync(string audioFile)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var audioFileReader = new AudioFileReader(audioFile);
                    
                    if (_audioDeviceService != null)
                    {
                        var selectedDevice = _audioDeviceService.GetSelectedDevice();
                        if (selectedDevice != null && selectedDevice.DeviceId != "default")
                        {
                            // Воспроизводим через выбранное устройство
                            var deviceId = int.Parse(selectedDevice.DeviceId);
                            using var waveOut = new WaveOut { DeviceNumber = deviceId };
                            waveOut.Init(audioFileReader);
                            waveOut.Play();
                            
                            // Ждем окончания воспроизведения
                            while (waveOut.PlaybackState == PlaybackState.Playing)
                            {
                                Task.Delay(50).Wait();
                            }
                        }
                        else
                        {
                            // Воспроизводим через устройство по умолчанию
                            using var waveOut = new WaveOut();
                            waveOut.Init(audioFileReader);
                            waveOut.Play();
                            
                            // Ждем окончания воспроизведения
                            while (waveOut.PlaybackState == PlaybackState.Playing)
                            {
                                Task.Delay(50).Wait();
                            }
                        }
                    }
                    else
                    {
                        // Воспроизводим через устройство по умолчанию
                        using var waveOut = new WaveOut();
                        waveOut.Init(audioFileReader);
                        waveOut.Play();
                        
                        // Ждем окончания воспроизведения
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            Task.Delay(50).Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения аудио файла: {ex.Message}");
                    throw;
                }
            });
        }

        public List<string> GetAvailableVoices()
        {
            return _synthesizer.GetInstalledVoices()
                .Where(v => v.Enabled)
                .Select(v => v.VoiceInfo.Name)
                .ToList();
        }

        public void SelectVoice(string voiceName)
        {
            if (!string.IsNullOrEmpty(voiceName))
            {
                try
                {
                    _synthesizer.SelectVoice(voiceName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка выбора голоса: {ex.Message}");
                }
            }
        }

        public void SetRate(int rate)
        {
            _synthesizer.Rate = rate;
        }

        public void SetVolume(int volume)
        {
            _synthesizer.Volume = volume;
        }

        public void UpdateSettings(VoiceSettings settings)
        {
            // Применяем настройки голоса
            if (!string.IsNullOrEmpty(settings.SelectedVoice))
            {
                SelectVoice(settings.SelectedVoice);
            }
            SetRate((int)settings.Rate);
            SetVolume((int)settings.Volume);
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
            // В упрощенной версии просто применяем профиль
            SelectVoice(profile.VoiceName);
            SetRate((int)profile.Rate);
            SetVolume((int)profile.Volume);
        }

        public void RemoveVoiceProfile(int index)
        {
            // В упрощенной версии ничего не делаем
        }

        public void UpdateVoiceProfile(int index, VoiceProfile profile)
        {
            // В упрощенной версии просто применяем профиль
            SelectVoice(profile.VoiceName);
            SetRate((int)profile.Rate);
            SetVolume((int)profile.Volume);
        }

        public ExternalTTSService GetExternalTTS()
        {
            // Возвращаем пустой сервис, так как в AdvancedSpeechService нет поддержки внешнего TTS
            return new ExternalTTSService();
        }

        public void Dispose()
        {
            _synthesizer?.Dispose();
        }
    }
} 