using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace SilentCaster.Services
{
    public class SubtitlesService
    {
        private readonly OBSService _obsService;
        private readonly Timer _subtitleTimer;
        private string _currentSubtitle = "";
        private string _subtitleSourceName = "Subtitles";
        private bool _isEnabled = false;
        private int _subtitleDuration = 5000; // 5 секунд по умолчанию
        private int _maxSubtitleLength = 100; // Максимальная длина субтитров

        public event EventHandler<string>? SubtitleChanged;
        public event EventHandler? SubtitleCleared;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (!value)
                {
                    ClearSubtitleAsync();
                }
            }
        }

        public string SubtitleSourceName
        {
            get => _subtitleSourceName;
            set => _subtitleSourceName = value;
        }

        public int SubtitleDuration
        {
            get => _subtitleDuration;
            set => _subtitleDuration = Math.Max(1000, Math.Min(30000, value)); // От 1 до 30 секунд
        }

        public int MaxSubtitleLength
        {
            get => _maxSubtitleLength;
            set => _maxSubtitleLength = Math.Max(10, Math.Min(500, value)); // От 10 до 500 символов
        }

        public SubtitlesService(OBSService obsService)
        {
            _obsService = obsService;
            _subtitleTimer = new Timer();
            _subtitleTimer.Elapsed += OnSubtitleTimerElapsed;
            _subtitleTimer.AutoReset = false;
        }

        public async Task ShowSubtitleAsync(string text, string? username = null)
        {
            if (!IsEnabled) return;

            try
            {
                // Формируем текст субтитров
                var subtitleText = FormatSubtitleText(text, username);
                
                // Обрезаем текст, если он слишком длинный
                if (subtitleText.Length > MaxSubtitleLength)
                {
                    subtitleText = subtitleText.Substring(0, MaxSubtitleLength - 3) + "...";
                }

                // Останавливаем предыдущий таймер
                _subtitleTimer.Stop();

                // Устанавливаем текст в OBS
                if (_obsService.IsEnabled && _obsService.IsConnected)
                {
                    await _obsService.SetTextAsync(SubtitleSourceName, subtitleText);
                    await _obsService.ShowSourceAsync(SubtitleSourceName);
                }

                _currentSubtitle = subtitleText;
                SubtitleChanged?.Invoke(this, subtitleText);

                // Запускаем таймер для автоматического скрытия
                _subtitleTimer.Interval = SubtitleDuration;
                _subtitleTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка показа субтитров: {ex.Message}");
            }
        }

        public async Task ClearSubtitleAsync()
        {
            if (!IsEnabled) return;

            try
            {
                _subtitleTimer.Stop();
                _currentSubtitle = "";

                // Скрываем источник в OBS
                if (_obsService.IsEnabled && _obsService.IsConnected)
                {
                    await _obsService.HideSourceAsync(SubtitleSourceName);
                }

                SubtitleCleared?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка очистки субтитров: {ex.Message}");
            }
        }

        public async Task ShowEmotionalSubtitleAsync(string emotionText, string? username = null)
        {
            if (!IsEnabled) return;

            try
            {
                var subtitleText = FormatEmotionalSubtitleText(emotionText, username);
                
                // Обрезаем текст, если он слишком длинный
                if (subtitleText.Length > MaxSubtitleLength)
                {
                    subtitleText = subtitleText.Substring(0, MaxSubtitleLength - 3) + "...";
                }

                // Останавливаем предыдущий таймер
                _subtitleTimer.Stop();

                // Устанавливаем текст в OBS с эмоциональным стилем
                if (_obsService.IsEnabled && _obsService.IsConnected)
                {
                    await _obsService.SetTextAsync(SubtitleSourceName, subtitleText);
                    await _obsService.ShowSourceAsync(SubtitleSourceName);
                }

                _currentSubtitle = subtitleText;
                SubtitleChanged?.Invoke(this, subtitleText);

                // Эмоциональные субтитры показываются дольше
                _subtitleTimer.Interval = SubtitleDuration + 2000; // +2 секунды
                _subtitleTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка показа эмоциональных субтитров: {ex.Message}");
            }
        }

        public async Task ShowEventSubtitleAsync(string eventText, string? username = null)
        {
            if (!IsEnabled) return;

            try
            {
                var subtitleText = FormatEventSubtitleText(eventText, username);
                
                // Обрезаем текст, если он слишком длинный
                if (subtitleText.Length > MaxSubtitleLength)
                {
                    subtitleText = subtitleText.Substring(0, MaxSubtitleLength - 3) + "...";
                }

                // Останавливаем предыдущий таймер
                _subtitleTimer.Stop();

                // Устанавливаем текст в OBS с событийным стилем
                if (_obsService.IsEnabled && _obsService.IsConnected)
                {
                    await _obsService.SetTextAsync(SubtitleSourceName, subtitleText);
                    await _obsService.ShowSourceAsync(SubtitleSourceName);
                }

                _currentSubtitle = subtitleText;
                SubtitleChanged?.Invoke(this, subtitleText);

                // Событийные субтитры показываются еще дольше
                _subtitleTimer.Interval = SubtitleDuration + 3000; // +3 секунды
                _subtitleTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка показа событийных субтитров: {ex.Message}");
            }
        }

        private string FormatSubtitleText(string text, string? username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return text;
            }

            return $"{username}: {text}";
        }

        private string FormatEmotionalSubtitleText(string text, string? username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return $"😊 {text}";
            }

            return $"😊 {username}: {text}";
        }

        private string FormatEventSubtitleText(string text, string? username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return $"🎉 {text}";
            }

            return $"🎉 {username}: {text}";
        }

        private async void OnSubtitleTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await ClearSubtitleAsync();
        }

        public string GetCurrentSubtitle()
        {
            return _currentSubtitle;
        }

        public void Dispose()
        {
            _subtitleTimer?.Dispose();
        }
    }
} 