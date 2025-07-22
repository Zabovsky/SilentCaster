using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class OBSIntegrationWindow : Window
    {
        private readonly OBSService _obsService;
        private readonly SubtitlesService _subtitlesService;
        private OBSIntegrationSettings _settings;

        public OBSIntegrationWindow(OBSService obsService, SubtitlesService subtitlesService)
        {
            InitializeComponent();
            _obsService = obsService;
            _subtitlesService = subtitlesService;
            
            LoadSettings();
            SetupEventHandlers();
            UpdateUI();
            _obsService.VersionDetected += OnOBSVersionDetected;
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("obs_integration_settings.json"))
                {
                    var json = File.ReadAllText("obs_integration_settings.json");
                    _settings = JsonConvert.DeserializeObject<OBSIntegrationSettings>(json) ?? new OBSIntegrationSettings();
                }
                else
                {
                    _settings = new OBSIntegrationSettings();
                }
            }
            catch
            {
                _settings = new OBSIntegrationSettings();
            }

            // Применяем настройки к UI
            OBSEnabledCheckBox.IsChecked = _settings.OBSEnabled;
            OBSUrlTextBox.Text = _settings.OBSUrl;
            OBSPasswordBox.Password = _settings.OBSPassword;
            
            SubtitlesEnabledCheckBox.IsChecked = _settings.SubtitlesEnabled;
            SubtitleSourceTextBox.Text = _settings.SubtitleSourceName;
            SubtitleDurationSlider.Value = _settings.SubtitleDuration / 1000.0; // Конвертируем в секунды
            SubtitleLengthSlider.Value = _settings.MaxSubtitleLength;
            
            ChatSubtitlesCheckBox.IsChecked = _settings.ChatSubtitlesEnabled;
            EmotionalSubtitlesCheckBox.IsChecked = _settings.EmotionalSubtitlesEnabled;
            EventSubtitlesCheckBox.IsChecked = _settings.EventSubtitlesEnabled;
            QuickResponseSubtitlesCheckBox.IsChecked = _settings.QuickResponseSubtitlesEnabled;
        }

        private void SetupEventHandlers()
        {
            // OBS Service события
            _obsService.ConnectionStatusChanged += OnOBSConnectionStatusChanged;
            _obsService.ErrorOccurred += OnOBSErrorOccurred;

            // Subtitles Service события
            _subtitlesService.SubtitleChanged += OnSubtitleChanged;
            _subtitlesService.SubtitleCleared += OnSubtitleCleared;

            // UI события
            SubtitleDurationSlider.ValueChanged += (s, e) => 
            {
                SubtitleDurationText.Text = $"{(int)e.NewValue} секунд";
            };

            SubtitleLengthSlider.ValueChanged += (s, e) => 
            {
                SubtitleLengthText.Text = $"{(int)e.NewValue} символов";
            };

            OBSEnabledCheckBox.Checked += (s, e) => UpdateOBSEnabled();
            OBSEnabledCheckBox.Unchecked += (s, e) => UpdateOBSEnabled();
            
            SubtitlesEnabledCheckBox.Checked += (s, e) => UpdateSubtitlesEnabled();
            SubtitlesEnabledCheckBox.Unchecked += (s, e) => UpdateSubtitlesEnabled();
        }

        private void UpdateOBSEnabled()
        {
            _obsService.IsEnabled = OBSEnabledCheckBox.IsChecked == true;
            UpdateUI();
        }

        private void UpdateSubtitlesEnabled()
        {
            _subtitlesService.IsEnabled = SubtitlesEnabledCheckBox.IsChecked == true;
            UpdateUI();
        }

        private void UpdateUI()
        {
            var obsEnabled = OBSEnabledCheckBox.IsChecked == true;
            var subtitlesEnabled = SubtitlesEnabledCheckBox.IsChecked == true;

            // OBS элементы
            OBSUrlTextBox.IsEnabled = true; // всегда доступно
            OBSPasswordBox.IsEnabled = true; // всегда доступно
            ConnectOBSButton.IsEnabled = obsEnabled && !_obsService.IsConnected;
            DisconnectOBSButton.IsEnabled = obsEnabled && _obsService.IsConnected;
            TestOBSButton.IsEnabled = obsEnabled && _obsService.IsConnected;

            // Субтитры элементы
            SubtitleSourceTextBox.IsEnabled = subtitlesEnabled;
            SubtitleDurationSlider.IsEnabled = subtitlesEnabled;
            SubtitleLengthSlider.IsEnabled = subtitlesEnabled;
            TestSubtitleButton.IsEnabled = subtitlesEnabled;
            ClearSubtitleButton.IsEnabled = subtitlesEnabled;
            
            ChatSubtitlesCheckBox.IsEnabled = subtitlesEnabled;
            EmotionalSubtitlesCheckBox.IsEnabled = subtitlesEnabled;
            EventSubtitlesCheckBox.IsEnabled = subtitlesEnabled;
            QuickResponseSubtitlesCheckBox.IsEnabled = subtitlesEnabled;
        }

        private void OnOBSConnectionStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                OBSStatusTextBlock.Text = status;
                UpdateUI();
            });
        }

        private void OnOBSErrorOccurred(object? sender, string error)
        {
            Dispatcher.Invoke(() =>
            {
                OBSStatusTextBlock.Text = $"Ошибка: {error}";
                OBSStatusTextBlock.Foreground = Brushes.Red;
            });
        }

        private void OnSubtitleChanged(object? sender, string subtitle)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentSubtitleText.Text = $"Текущие субтитры: {subtitle}";
            });
        }

        private void OnSubtitleCleared(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentSubtitleText.Text = "Текущие субтитры: нет";
            });
        }

        private async void ConnectOBSButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectOBSButton.IsEnabled = false;
                OBSStatusTextBlock.Text = "Подключение...";
                OBSStatusTextBlock.Foreground = Brushes.Yellow;

                string url = OBSUrlTextBox.Text.Trim();
                if (!url.StartsWith("ws://") && !url.StartsWith("wss://"))
                    url = "ws://" + url;
                try
                {
                    var uri = new Uri(url); // Проверка формата
                }
                catch (UriFormatException)
                {
                    OBSStatusTextBlock.Text = "Ошибка: некорректный адрес OBS (должен быть ws://host:port)";
                    OBSStatusTextBlock.Foreground = Brushes.Red;
                    ConnectOBSButton.IsEnabled = true;
                    return;
                }

                var success = await _obsService.ConnectAsync(url, OBSPasswordBox.Password);
                if (success)
                {
                    OBSStatusTextBlock.Text = "Подключено к OBS";
                    OBSStatusTextBlock.Foreground = Brushes.Green;
                }
                else
                {
                    OBSStatusTextBlock.Text = "Ошибка подключения";
                    OBSStatusTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                OBSStatusTextBlock.Text = $"Ошибка: {ex.Message}";
                OBSStatusTextBlock.Foreground = Brushes.Red;
            }
            finally
            {
                UpdateUI();
            }
        }

        private async void DisconnectOBSButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _obsService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                OBSStatusTextBlock.Text = $"Ошибка отключения: {ex.Message}";
                OBSStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private async void TestOBSButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = await _obsService.SetTextAsync(SubtitleSourceTextBox.Text, "🧪 Тест OBS интеграции");
                if (success)
                {
                    OBSStatusTextBlock.Text = "Тест успешен!";
                    OBSStatusTextBlock.Foreground = Brushes.Green;
                }
                else
                {
                    OBSStatusTextBlock.Text = "Тест не удался";
                    OBSStatusTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                OBSStatusTextBlock.Text = $"Ошибка теста: {ex.Message}";
                OBSStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private async void TestSubtitleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _subtitlesService.ShowSubtitleAsync("Это тестовое сообщение для проверки субтитров!", "Тестер");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка теста субтитров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearSubtitleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _subtitlesService.ClearSubtitleAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки субтитров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем настройки
                _settings.OBSEnabled = OBSEnabledCheckBox.IsChecked == true;
                _settings.OBSUrl = OBSUrlTextBox.Text;
                _settings.OBSPassword = OBSPasswordBox.Password;
                
                _settings.SubtitlesEnabled = SubtitlesEnabledCheckBox.IsChecked == true;
                _settings.SubtitleSourceName = SubtitleSourceTextBox.Text;
                _settings.SubtitleDuration = (int)(SubtitleDurationSlider.Value * 1000); // Конвертируем в миллисекунды
                _settings.MaxSubtitleLength = (int)SubtitleLengthSlider.Value;
                
                _settings.ChatSubtitlesEnabled = ChatSubtitlesCheckBox.IsChecked == true;
                _settings.EmotionalSubtitlesEnabled = EmotionalSubtitlesCheckBox.IsChecked == true;
                _settings.EventSubtitlesEnabled = EventSubtitlesCheckBox.IsChecked == true;
                _settings.QuickResponseSubtitlesEnabled = QuickResponseSubtitlesCheckBox.IsChecked == true;

                // Применяем настройки к сервисам
                _obsService.IsEnabled = _settings.OBSEnabled;
                _subtitlesService.IsEnabled = _settings.SubtitlesEnabled;
                _subtitlesService.SubtitleSourceName = _settings.SubtitleSourceName;
                _subtitlesService.SubtitleDuration = _settings.SubtitleDuration;
                _subtitlesService.MaxSubtitleLength = _settings.MaxSubtitleLength;

                // Сохраняем в файл
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText("obs_integration_settings.json", json);

                MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Отписываемся от событий
            _obsService.ConnectionStatusChanged -= OnOBSConnectionStatusChanged;
            _obsService.ErrorOccurred -= OnOBSErrorOccurred;
            _subtitlesService.SubtitleChanged -= OnSubtitleChanged;
            _subtitlesService.SubtitleCleared -= OnSubtitleCleared;
            
            base.OnClosed(e);
        }

        private void OnOBSVersionDetected(object? sender, string version)
        {
            Dispatcher.Invoke(() =>
            {
                OBSStatusTextBlock.Text = $"Подключено к OBS (версия: {version})";
                OBSStatusTextBlock.Foreground = Brushes.Green;
            });
        }
    }

    public class OBSIntegrationSettings
    {
        public bool OBSEnabled { get; set; } = false;
        public string OBSUrl { get; set; } = "ws://localhost:4444";
        public string OBSPassword { get; set; } = "";
        
        public bool SubtitlesEnabled { get; set; } = false;
        public string SubtitleSourceName { get; set; } = "Subtitles";
        public int SubtitleDuration { get; set; } = 5000; // миллисекунды
        public int MaxSubtitleLength { get; set; } = 100;
        
        public bool ChatSubtitlesEnabled { get; set; } = true;
        public bool EmotionalSubtitlesEnabled { get; set; } = true;
        public bool EventSubtitlesEnabled { get; set; } = true;
        public bool QuickResponseSubtitlesEnabled { get; set; } = true;
    }
} 