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
    public class AdvancedSpeechService : IDisposable
    {
        private SpeechSynthesizer? _synthesizer;
        private readonly AudioDeviceService? _audioDeviceService;
        private readonly Random _random;
        private VoiceSettings _voiceSettings;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public AdvancedSpeechService(AudioDeviceService? audioDeviceService = null)
        {
            _audioDeviceService = audioDeviceService;
            _random = new Random();
            _voiceSettings = new VoiceSettings();
            InitializeSynthesizer();
        }

        private void InitializeSynthesizer()
        {
            lock (_lockObject)
            {
                if (_disposed) return;
                
                try
                {
                    // Безопасно освобождаем старый синтезатор
                    if (_synthesizer != null)
                    {
                        try
                        {
                            _synthesizer.Dispose();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Синтезатор уже был освобожден, это нормально
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка освобождения синтезатора: {ex.Message}");
                        }
                        _synthesizer = null;
                    }
                    
                    // Создаем новый синтезатор
                    _synthesizer = new SpeechSynthesizer();
                    
                    // Проверяем, что синтезатор работает
                    var state = _synthesizer.State;
                    System.Diagnostics.Debug.WriteLine($"Синтезатор инициализирован, состояние: {state}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка инициализации синтезатора: {ex.Message}");
                    _synthesizer = null;
                }
            }
        }

        private SpeechSynthesizer? GetSynthesizer()
        {
            lock (_lockObject)
            {
                if (_disposed) return null;
                
                try
                {
                    // Проверяем, не закрыт ли синтезатор
                    if (_synthesizer != null)
                    {
                        // Пытаемся получить состояние - если синтезатор закрыт, это вызовет исключение
                        var state = _synthesizer.State;
                    }
                    else
                    {
                        InitializeSynthesizer();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Синтезатор был закрыт, создаем новый
                    System.Diagnostics.Debug.WriteLine("Синтезатор был закрыт, создаем новый");
                    InitializeSynthesizer();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка проверки состояния синтезатора: {ex.Message}");
                    InitializeSynthesizer();
                }
                
                return _synthesizer;
            }
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
                    var synthesizer = GetSynthesizer();
                    if (synthesizer == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Синтезатор недоступен");
                        return;
                    }

                    // Дополнительная проверка состояния синтезатора
                    try
                    {
                        var state = synthesizer.State;
                    }
                    catch (ObjectDisposedException)
                    {
                        System.Diagnostics.Debug.WriteLine("Синтезатор был закрыт во время использования");
                        return;
                    }

                    // Выбираем профиль голоса в зависимости от настроек
                    var selectedProfile = SelectVoiceProfile(messageType);
                    
                    // Применяем настройки выбранного профиля
                    if (selectedProfile != null)
                    {
                        SelectVoice(selectedProfile.VoiceName);
                        SetRate((int)selectedProfile.Rate);
                        SetVolume((int)selectedProfile.Volume);
                    }
                    
                    await SpeakWithSelectedDeviceAsync(processedText);
                }
                catch (ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine("Синтезатор был закрыт во время синтеза речи");
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
                var synthesizer = GetSynthesizer();
                if (synthesizer == null)
                {
                    System.Diagnostics.Debug.WriteLine("Синтезатор недоступен для воспроизведения");
                    return;
                }

                // Создаем временный файл для аудио
                var tempFile = Path.GetTempFileName();
                tempFile = Path.ChangeExtension(tempFile, ".wav");

                // Синтезируем речь в файл
                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    synthesizer.SetOutputToWaveStream(stream);
                    synthesizer.Speak(text);
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
                try
                {
                    var synthesizer = GetSynthesizer();
                    if (synthesizer != null)
                    {
                        synthesizer.SetOutputToDefaultAudioDevice();
                        synthesizer.Speak(text);
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка стандартного воспроизведения: {fallbackEx.Message}");
                }
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
            var synthesizer = GetSynthesizer();
            if (synthesizer == null) return new List<string>();

            try
            {
                return synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения доступных голосов: {ex.Message}");
                return new List<string>();
            }
        }

        public void SelectVoice(string voiceName)
        {
            if (!string.IsNullOrEmpty(voiceName))
            {
                try
                {
                    var synthesizer = GetSynthesizer();
                    synthesizer?.SelectVoice(voiceName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка выбора голоса: {ex.Message}");
                }
            }
        }

        public void SetRate(int rate)
        {
            try
            {
                var synthesizer = GetSynthesizer();
                if (synthesizer != null)
                {
                    synthesizer.Rate = rate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки скорости: {ex.Message}");
            }
        }

        public void SetVolume(int volume)
        {
            try
            {
                var synthesizer = GetSynthesizer();
                if (synthesizer != null)
                {
                    synthesizer.Volume = volume;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки громкости: {ex.Message}");
            }
        }

        public void UpdateSettings(VoiceSettings settings)
        {
            _voiceSettings = settings;
            
            // Применяем настройки голоса
            if (!string.IsNullOrEmpty(settings.SelectedVoice))
            {
                SelectVoice(settings.SelectedVoice);
            }
            SetRate((int)settings.Rate);
            SetVolume((int)settings.Volume);
        }

        private VoiceProfile? SelectVoiceProfile(string messageType)
        {
            // Если множественные голоса отключены, используем базовые настройки
            if (!_voiceSettings.UseMultipleVoices)
            {
                return null;
            }

            // Фильтруем активные профили по типу сообщения
            var availableProfiles = _voiceSettings.VoiceProfiles
                .Where(p => p.IsEnabled)
                .Where(p =>
                    // Если ни одна галочка не стоит — профиль подходит для всех типов
                    (!p.UseForChatMessages && !p.UseForQuickResponses && !p.UseForManualMessages)
                    ||
                    // Иначе — профиль подходит только для выбранных типов
                    (messageType == "chat" && p.UseForChatMessages) ||
                    (messageType == "quick" && p.UseForQuickResponses) ||
                    (messageType == "manual" && p.UseForManualMessages)
                )
                .ToList();

            if (!availableProfiles.Any())
            {
                return null;
            }

            // Сортируем по приоритету (высокий приоритет = низкий номер)
            availableProfiles = availableProfiles.OrderBy(p => p.Priority).ToList();

            // Выбираем профиль с учетом шанса использования
            foreach (var profile in availableProfiles)
            {
                if (_random.NextDouble() * 100 <= profile.UsageChance)
                {
                    return profile;
                }
            }

            // Если ни один профиль не выбран по шансу, возвращаем первый с наивысшим приоритетом
            return availableProfiles.FirstOrDefault();
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
            lock (_lockObject)
            {
                if (_disposed) return;
                
                try
                {
                    if (_synthesizer != null)
                    {
                        try
                        {
                            _synthesizer.Dispose();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Синтезатор уже был освобожден, это нормально
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка освобождения синтезатора в Dispose: {ex.Message}");
                        }
                        _synthesizer = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка в Dispose: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
} 