using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class ExternalTTSWindow : Window
    {
        private readonly ExternalTTSService _externalTTS;
        private ExternalTTSService.ExternalTTSConfig _config;
        private ExternalTTSService.ExternalTTSProvider? _selectedProvider;
        private bool _isEditing = false;

        // Предустановленные конфигурации для разных типов TTS
        private readonly Dictionary<string, (string path, string args)> _presetConfigs = new()
        {
            ["TTS Voice Wizard"] = (
                @"C:\Program Files\TTS Voice Wizard\TTSVoiceWizard.exe",
                "-voice \"{voice}\" -rate {rate} -volume {volume} -text \"{text}\""
            ),
            ["eSpeak"] = (
                @"C:\Program Files\eSpeak\command_line\espeak.exe",
                "-v {voice} -s {rate} -a {volume} \"{text}\""
            ),
            ["RHVoice"] = (
                @"C:\Program Files\RHVoice\RHVoice.exe",
                "--voice {voice} --rate {rate} --volume {volume} --text \"{text}\""
            ),
            ["Balabolka"] = (
                @"C:\Program Files\Balabolka\balcon.exe",
                "-n \"{voice}\" -s {rate} -v {volume} -t \"{text}\""
            )
        };

        public ExternalTTSWindow(ExternalTTSService externalTTS)
        {
            InitializeComponent();
            _externalTTS = externalTTS;
            _config = _externalTTS.GetConfig();
            
            LoadConfig();
            SetupEventHandlers();
            UpdateStatus();
        }

        private void LoadConfig()
        {
            // Загружаем общие настройки
            UseExternalTTSCheckBox.IsChecked = _config.UseExternalTTS;
            
            // Загружаем список провайдеров
            ProvidersListBox.ItemsSource = _config.Providers;
            
            // Загружаем провайдеров по умолчанию
            UpdateDefaultProviderComboBox();
            
            if (!string.IsNullOrEmpty(_config.DefaultProvider))
            {
                DefaultProviderComboBox.SelectedItem = _config.DefaultProvider;
            }
        }

        private void UpdateDefaultProviderComboBox()
        {
            var enabledProviders = _config.Providers.Where(p => p.IsEnabled).Select(p => p.Name).ToList();
            DefaultProviderComboBox.ItemsSource = enabledProviders;
        }

        private void SetupEventHandlers()
        {
            ProvidersListBox.SelectionChanged += ProvidersListBox_SelectionChanged;
            ProviderTypeComboBox.SelectionChanged += ProviderTypeComboBox_SelectionChanged;
        }

        private void UpdateStatus()
        {
            if (_config.UseExternalTTS && !string.IsNullOrEmpty(_config.DefaultProvider))
            {
                var provider = _config.Providers.FirstOrDefault(p => p.Name == _config.DefaultProvider);
                if (provider != null && File.Exists(provider.ExecutablePath))
                {
                    StatusTextBlock.Text = "Активен";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightGreen;
                    StatusBorder.Background = System.Windows.Media.Brushes.DarkGreen;
                }
                else
                {
                    StatusTextBlock.Text = "Ошибка конфигурации";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightCoral;
                    StatusBorder.Background = System.Windows.Media.Brushes.DarkRed;
                }
            }
            else
            {
                StatusTextBlock.Text = "Неактивен";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                StatusBorder.Background = System.Windows.Media.Brushes.DarkGray;
            }
        }

        private void UseExternalTTSCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _config.UseExternalTTS = true;
            DefaultProviderComboBox.IsEnabled = true;
            UpdateStatus();
        }

        private void UseExternalTTSCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _config.UseExternalTTS = false;
            DefaultProviderComboBox.IsEnabled = false;
            UpdateStatus();
        }

        private void AddProviderButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = false;
            _selectedProvider = new ExternalTTSService.ExternalTTSProvider
            {
                Name = "Новый провайдер",
                Type = "TTS Voice Wizard",
                IsEnabled = true,
                Voices = new List<ExternalTTSService.ExternalVoice>()
            };
            
            ShowProviderSettings();
        }

        private void EditProviderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProvidersListBox.SelectedItem is ExternalTTSService.ExternalTTSProvider provider)
            {
                _isEditing = true;
                _selectedProvider = provider;
                ShowProviderSettings();
            }
            else
            {
                MessageBox.Show("Выберите провайдер для редактирования.", "Внимание", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveProviderButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProvidersListBox.SelectedItem is ExternalTTSService.ExternalTTSProvider provider)
            {
                var result = MessageBox.Show($"Удалить провайдер '{provider.Name}'?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _config.Providers.Remove(provider);
                    ProvidersListBox.Items.Refresh();
                    UpdateDefaultProviderComboBox();
                    HideProviderSettings();
                    UpdateStatus();
                }
            }
            else
            {
                MessageBox.Show("Выберите провайдер для удаления.", "Внимание", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ProvidersListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProvidersListBox.SelectedItem is ExternalTTSService.ExternalTTSProvider provider)
            {
                _selectedProvider = provider;
                ShowProviderSettings();
            }
            else
            {
                HideProviderSettings();
            }
        }

        private void ProviderTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProviderTypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                var selectedType = selectedItem.Content.ToString();
                if (_presetConfigs.ContainsKey(selectedType))
                {
                    var (path, args) = _presetConfigs[selectedType];
                    ProviderPathTextBox.Text = path;
                    ProviderArgsTextBox.Text = args;
                }
            }
        }

        private void ShowProviderSettings()
        {
            if (_selectedProvider == null) return;

            ProviderSettingsBorder.Visibility = Visibility.Visible;
            NoProviderBorder.Visibility = Visibility.Collapsed;
            
            ProviderSettingsTitle.Text = _selectedProvider.Name;
            ProviderNameTextBox.Text = _selectedProvider.Name;
            ProviderPathTextBox.Text = _selectedProvider.ExecutablePath;
            ProviderArgsTextBox.Text = _selectedProvider.Arguments;
            
            // Устанавливаем тип
            foreach (System.Windows.Controls.ComboBoxItem item in ProviderTypeComboBox.Items)
            {
                if (item.Content.ToString() == _selectedProvider.Type)
                {
                    ProviderTypeComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Загружаем голоса
            ProviderVoicesListBox.ItemsSource = _selectedProvider.Voices;
        }

        private void HideProviderSettings()
        {
            ProviderSettingsBorder.Visibility = Visibility.Collapsed;
            NoProviderBorder.Visibility = Visibility.Visible;
            _selectedProvider = null;
        }

        private void BrowsePathButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите исполняемый файл TTS движка",
                Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProviderPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void TestPathButton_Click(object sender, RoutedEventArgs e)
        {
            var path = ProviderPathTextBox.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Укажите путь к исполняемому файлу.", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (File.Exists(path))
            {
                MessageBox.Show("Файл найден!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Файл не найден. Проверьте путь.", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProvider == null) return;

            var voice = new ExternalTTSService.ExternalVoice
            {
                Name = "Новый голос",
                VoiceId = "voice_id",
                Language = "ru-RU",
                Rate = 0,
                Volume = 100
            };

            _selectedProvider.Voices.Add(voice);
            ProviderVoicesListBox.Items.Refresh();
        }

        private void EditVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProvider == null || ProviderVoicesListBox.SelectedItem is not ExternalTTSService.ExternalVoice voice) 
                return;

            // Здесь можно добавить диалог редактирования голоса
            MessageBox.Show("Функция редактирования голоса будет добавлена в следующей версии.", "Информация", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProvider == null || ProviderVoicesListBox.SelectedItem is not ExternalTTSService.ExternalVoice voice) 
                return;

            var result = MessageBox.Show($"Удалить голос '{voice.Name}'?", "Подтверждение", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _selectedProvider.Voices.Remove(voice);
                ProviderVoicesListBox.Items.Refresh();
            }
        }

        private async void TestProviderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProvider == null) return;

            try
            {
                var success = await _externalTTS.SpeakAsync("Тест внешнего TTS движка", 
                    _selectedProvider.Voices.FirstOrDefault()?.Name ?? "", 0, 100);
                
                if (success)
                {
                    MessageBox.Show("Тест успешно выполнен!", "Результат", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при выполнении теста. Проверьте настройки.", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProviderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProvider == null) return;

            try
            {
                _selectedProvider.Name = ProviderNameTextBox.Text;
                _selectedProvider.ExecutablePath = ProviderPathTextBox.Text;
                _selectedProvider.Arguments = ProviderArgsTextBox.Text;
                
                if (ProviderTypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedType)
                {
                    _selectedProvider.Type = selectedType.Content.ToString() ?? "";
                }

                if (!_isEditing)
                {
                    _config.Providers.Add(_selectedProvider);
                }

                ProvidersListBox.Items.Refresh();
                UpdateDefaultProviderComboBox();
                UpdateStatus();
                
                MessageBox.Show("Провайдер сохранен!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем общие настройки
                _config.UseExternalTTS = UseExternalTTSCheckBox.IsChecked ?? false;
                _config.DefaultProvider = DefaultProviderComboBox.SelectedItem?.ToString() ?? "";

                // Обновляем конфигурацию
                _externalTTS.UpdateConfig(_config);
                
                MessageBox.Show("Настройки сохранены!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Управление окном
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
            DialogResult = false;
            Close();
        }
    }
} 