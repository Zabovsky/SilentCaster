using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class MainWindow : Window
    {
        private readonly TwitchService _twitchService;
        private readonly AdvancedSpeechService _speechService;
        private readonly ResponseService _responseService;
        private readonly SettingsService _settingsService;
        private readonly AudioDeviceService _audioDeviceService;
        private readonly ObservableCollection<ChatMessage> _chatMessages;
        private readonly ObservableCollection<QuickResponse> _responses;
        private VoiceSettings _voiceSettings;
        private AppSettings _appSettings;

        public MainWindow()
        {
            InitializeComponent();
            
            _twitchService = new TwitchService();
            _audioDeviceService = new AudioDeviceService();
            _speechService = new AdvancedSpeechService(_audioDeviceService);
            _responseService = new ResponseService();
            _settingsService = new SettingsService();
            
            _chatMessages = new ObservableCollection<ChatMessage>();
            _responses = new ObservableCollection<QuickResponse>();
            _voiceSettings = new VoiceSettings();
            
            // Добавляем профили по умолчанию, если их нет
            if (_voiceSettings.VoiceProfiles.Count == 0)
            {
                _voiceSettings.VoiceProfiles.Add(new VoiceProfile
                {
                    Name = "Основной голос",
                    VoiceName = "Microsoft David Desktop",
                    Rate = 0.0,
                    Volume = 100.0,
                    IsEnabled = true,
                    Description = "Основной голос для озвучивания",
                    Priority = 1,
                    UsageChance = 100.0
                });
                
                _voiceSettings.VoiceProfiles.Add(new VoiceProfile
                {
                    Name = "Альтернативный голос",
                    VoiceName = "Microsoft Zira Desktop",
                    Rate = 0.0,
                    Volume = 100.0,
                    IsEnabled = true,
                    Description = "Альтернативный голос для разнообразия",
                    Priority = 2,
                    UsageChance = 50.0
                });
            }
            _appSettings = _settingsService.LoadSettings();
            
            ChatMessagesListBox.ItemsSource = _chatMessages;
            QuickResponsesListBox.ItemsSource = _responses;
            ActiveProfilesListBox.ItemsSource = _voiceSettings.VoiceProfiles;
            
            // Подписываемся на события
            _twitchService.MessageReceived += OnMessageReceived;
            _twitchService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            LoadResponses();
            LoadAppSettings();
            
            // Инициализация голосовых настроек после полной загрузки XAML
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeVoiceSettings();
            }));
        }

        private void InitializeVoiceSettings()
        {
            // Инициализируем настройки множественных голосов
            if (UseMultipleVoicesCheckBox != null)
            {
                UseMultipleVoicesCheckBox.IsChecked = _voiceSettings.UseMultipleVoices;
            }
            
            // Инициализируем настройки окна
            if (AlwaysOnTopCheckBox != null)
            {
                AlwaysOnTopCheckBox.IsChecked = _appSettings.AlwaysOnTop;
            }
            
            // Инициализируем настройки озвучки чата
            if (EnableChatVoiceCheckBox != null)
            {
                EnableChatVoiceCheckBox.IsChecked = _appSettings.EnableChatVoice;
            }
            if (ChatTriggerSymbolTextBox != null)
            {
                ChatTriggerSymbolTextBox.Text = _appSettings.ChatTriggerSymbol;
            }
            
            // Обновляем настройки голоса после полной инициализации
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateVoiceSettings();
            }));
        }

        private void LoadResponses()
        {
            _responses.Clear();
            var responses = _responseService.GetAllResponses();
            foreach (var response in responses)
            {
                _responses.Add(response);
            }
        }

        private void LoadAppSettings()
        {
            // Применяем настройки окна
            this.Topmost = _appSettings.AlwaysOnTop;
            this.Left = _appSettings.WindowLeft;
            this.Top = _appSettings.WindowTop;
            this.Width = _appSettings.WindowWidth;
            this.Height = _appSettings.WindowHeight;
            
            if (_appSettings.WindowMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            
            // Заполняем поля подключения
            UsernameTextBox.Text = _appSettings.LastUsername;
            ChannelTextBox.Text = _appSettings.LastChannel;
            
            // Загружаем голосовые настройки
            if (_appSettings.VoiceSettings != null)
            {
                // Копируем настройки, чтобы не нарушить привязку данных
                _voiceSettings.UseMultipleVoices = _appSettings.VoiceSettings.UseMultipleVoices;
                _voiceSettings.VoiceProfiles.Clear();
                foreach (var profile in _appSettings.VoiceSettings.VoiceProfiles)
                {
                    _voiceSettings.VoiceProfiles.Add(profile);
                }
            }
            else
            {
                // Применяем настройки голоса по умолчанию
                _voiceSettings.UseMultipleVoices = _appSettings.UseMultipleVoices;
            }
            
            // Загружаем настройки озвучки чата
            if (EnableChatVoiceCheckBox != null)
            {
                EnableChatVoiceCheckBox.IsChecked = _appSettings.EnableChatVoice;
            }
            if (ChatTriggerSymbolTextBox != null)
            {
                ChatTriggerSymbolTextBox.Text = _appSettings.ChatTriggerSymbol;
            }
        }

        private void UpdateVoiceSettings()
        {
            try
            {
                if (_speechService != null)
                {
                    _speechService.UpdateSettings(_voiceSettings);
                }
                
                // Обновляем привязку данных для списка активных профилей
                if (ActiveProfilesListBox != null)
                {
                    ActiveProfilesListBox.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу приложения
                System.Diagnostics.Debug.WriteLine($"UpdateVoiceSettings error: {ex.Message}");
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_twitchService.IsConnected)
            {
                try
                {
                    ConnectButton.IsEnabled = false;
                    ConnectButton.Content = "⏳ Отключение...";
                    
                    // Отключаемся от чата асинхронно
                    await _twitchService.DisconnectAsync();
                    
                    // Очищаем список сообщений
                    Dispatcher.Invoke(() =>
                    {
                        _chatMessages.Clear();
                        if (ChatCounterTextBlock != null)
                        {
                            ChatCounterTextBlock.Text = " (0)";
                        }
                    });
                    
                    ConnectButton.Content = "🔗 Подключиться";
                    StatusTextBlock.Text = "Отключено от чата";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectButton.Content = "❌ Отключиться";
                }
                finally
                {
                    ConnectButton.IsEnabled = true;
                }
                return;
            }

            var username = UsernameTextBox.Text.Trim();
            var channel = ChannelTextBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(channel))
            {
                MessageBox.Show("Пожалуйста, введите ваш никнейм и название канала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConnectButton.IsEnabled = false;
            ConnectButton.Content = "⏳ Подключение...";

            try
            {
                // Используем анонимное подключение без OAuth
                await _twitchService.ConnectAnonymouslyAsync(channel);
                
                if (_twitchService.IsConnected)
                {
                    ConnectButton.Content = "❌ Отключиться";
                    StatusTextBlock.Text = $"Подключен к каналу: {channel}";
                }
                else
                {
                    ConnectButton.Content = "🔗 Подключиться";
                    StatusTextBlock.Text = "Ошибка подключения";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButton.Content = "🔗 Подключиться";
                StatusTextBlock.Text = "Ошибка подключения";
            }

            ConnectButton.IsEnabled = true;
        }

        private async void OnMessageReceived(object? sender, ChatMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                _chatMessages.Add(message);
                
                // Сохраняем максимум 100 сообщений
                while (_chatMessages.Count > 100)
                {
                    _chatMessages.RemoveAt(0);
                }
                
                // Обновляем счетчик сообщений
                if (ChatCounterTextBlock != null)
                {
                    ChatCounterTextBlock.Text = $" ({_chatMessages.Count})";
                }
                
                // Автоматически прокручиваем вниз
                ChatMessagesListBox.ScrollIntoView(message);
            });

            // Проверяем настройки озвучки чата
            if (_appSettings?.EnableChatVoice == true)
            {
                // Проверяем, содержит ли сообщение символ триггера
                if (string.IsNullOrEmpty(_appSettings.ChatTriggerSymbol) || 
                    message.Message.Contains(_appSettings.ChatTriggerSymbol))
                {
                    // Озвучиваем сообщение чата
                    await _speechService.SpeakAsync(message.Message, message.Username, "chat");
                }
            }
        }

        private void OnConnectionStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = status;
            });
        }

        private void ChatMessagesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChatMessagesListBox.SelectedItem is ChatMessage selectedMessage)
            {
                ShowResponseContextMenu(selectedMessage);
            }
        }

        private void ShowResponseContextMenu(ChatMessage message)
        {
            var responses = _responseService.GetResponsesForMessage(message.Message);
            
            var contextMenu = new ContextMenu();
            
            foreach (var response in responses)
            {
                var menuItem = new MenuItem
                {
                    Header = response,
                    Tag = new ResponseData { Response = response, Username = message.Username }
                };
                menuItem.Click += ResponseMenuItem_Click;
                contextMenu.Items.Add(menuItem);
            }
            
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.PlacementTarget = ChatMessagesListBox;
                contextMenu.IsOpen = true;
            }
        }

        private async void ResponseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ResponseData data)
            {
                await _speechService.SpeakAsync(data.Response, data.Username, "quick");
            }
        }

        private class ResponseData
        {
            public string Response { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }

        private async void TestVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            await _speechService.SpeakTestAsync();
        }

        private async void QuickResponsesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuickResponsesListBox.SelectedItem is QuickResponse selectedResponse)
            {
                // Автоматически воспроизводим первый ответ при выборе
                if (selectedResponse.Responses.Any())
                {
                    var response = selectedResponse.Responses.First();
                    await _speechService.SpeakAsync(response, "Стример", "quick");
                }
            }
        }

        private async void SpeakMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                await _speechService.SpeakAsync(message, null, "manual");
            }
        }

        private void ClearMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = string.Empty;
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем временный SpeechService для совместимости
            var tempSpeechService = new SpeechService(_audioDeviceService);
            var settingsWindow = new SettingsWindow(_responseService, tempSpeechService);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            
            // Обновляем список после закрытия окна настроек
            LoadResponses();
        }

        private void UseMultipleVoicesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _voiceSettings.UseMultipleVoices = true;
            UpdateVoiceSettings();
            SaveAppSettings();
        }

        private void UseMultipleVoicesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _voiceSettings.UseMultipleVoices = false;
            UpdateVoiceSettings();
            SaveAppSettings();
        }

        private void AlwaysOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            _appSettings.AlwaysOnTop = true;
            SaveAppSettings();
        }

        private void AlwaysOnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            _appSettings.AlwaysOnTop = false;
            SaveAppSettings();
        }

        private void EnableChatVoiceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.EnableChatVoice = true;
                SaveAppSettings();
            }
        }

        private void EnableChatVoiceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.EnableChatVoice = false;
                SaveAppSettings();
            }
        }

        private void ChatTriggerSymbolTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && _appSettings != null)
            {
                _appSettings.ChatTriggerSymbol = textBox.Text;
                SaveAppSettings();
            }
        }

        private void OpenVoiceProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем временный SpeechService для совместимости
            var tempSpeechService = new SpeechService(_audioDeviceService);
            var voiceProfilesWindow = new VoiceProfilesWindow(tempSpeechService, _voiceSettings);
            voiceProfilesWindow.Owner = this;
            voiceProfilesWindow.ShowDialog();
            
            // Обновляем настройки после закрытия окна
            _voiceSettings = voiceProfilesWindow.GetUpdatedVoiceSettings(_voiceSettings);
            UpdateVoiceSettings();
            
            // Обновляем список активных профилей
            ActiveProfilesListBox.Items.Refresh();
            
            // Сохраняем настройки
            SaveAppSettings();
        }

        private void OpenExternalTTSButton_Click(object sender, RoutedEventArgs e)
        {
            var externalTTSWindow = new ExternalTTSWindow(_speechService.GetExternalTTS());
            externalTTSWindow.Owner = this;
            externalTTSWindow.ShowDialog();
            
            // Обновляем список голосов после изменений
            UpdateVoiceSettings();
        }

        private void OpenAudioDeviceSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_audioDeviceService == null)
                {
                    MessageBox.Show("Служба аудио устройств не инициализирована", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var audioDeviceSettingsWindow = new AudioDeviceSettingsWindow(_audioDeviceService);
                audioDeviceSettingsWindow.Owner = this;
                audioDeviceSettingsWindow.ShowDialog();
                
                // Обновляем настройки после закрытия окна
                UpdateVoiceSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия настроек аудио устройств: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuickResponsesListBox.SelectedItem is QuickResponse selectedResponse)
            {
                var index = _responses.IndexOf(selectedResponse);
                _responseService.RemoveResponse(index);
                LoadResponses();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Сохраняем настройки перед закрытием
            SaveAppSettings();
            
            // Отключаемся от чата синхронно при закрытии окна
            _twitchService.Disconnect();
            _speechService.Dispose();
            base.OnClosed(e);
        }

        private void SaveAppSettings()
        {
            if (_appSettings == null) return;
            
            // Сохраняем настройки окна
            _appSettings.WindowLeft = this.Left;
            _appSettings.WindowTop = this.Top;
            _appSettings.WindowWidth = this.Width;
            _appSettings.WindowHeight = this.Height;
            _appSettings.WindowMaximized = this.WindowState == WindowState.Maximized;
            
            // Сохраняем настройки подключения
            if (UsernameTextBox != null)
                _appSettings.LastUsername = UsernameTextBox.Text;
            if (ChannelTextBox != null)
                _appSettings.LastChannel = ChannelTextBox.Text;
            
            // Сохраняем голосовые настройки
            _appSettings.VoiceSettings = _voiceSettings;
            _appSettings.UseMultipleVoices = _voiceSettings.UseMultipleVoices;
            
            // Сохраняем в файл
            _settingsService.SaveSettings(_appSettings);
        }
        
        // Обработчики для управления окном
        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void OnMaximizeClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "□";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "❐";
            }
        }
        
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 