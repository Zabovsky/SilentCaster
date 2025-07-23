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
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using System.Windows.Navigation;

namespace SilentCaster
{
    public partial class MainWindow : Window
    {
        private readonly TwitchService _twitchService;
        private readonly AdvancedSpeechService _speechService;
        private readonly ResponseService _responseService;
        private readonly SettingsService _settingsService;
        private readonly AudioDeviceService _audioDeviceService;
        private readonly ForbiddenWordsService _forbiddenWordsService;
        private readonly EmotionalReactionsService _emotionalReactionsService;
        private readonly StreamEventsService _streamEventsService;
        private readonly OBSService _obsService;
        private readonly SubtitlesService _subtitlesService;
        private readonly ObservableCollection<ChatMessage> _chatMessages;
        private readonly ObservableCollection<QuickResponse> _responses;
        private readonly Random _random;
        private VoiceSettings _voiceSettings;
        private AppSettings _appSettings;
        private TwitchPubSub _pubSub;
        private string? _twitchUserName = null;
        private System.Timers.Timer? _twitchUserCheckTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ИНИЦИАЛИЗАЦИЯ ГЛАВНОГО ОКНА ===");
                
                _twitchService = new TwitchService();
                _audioDeviceService = new AudioDeviceService();
                _speechService = new AdvancedSpeechService(_audioDeviceService);
                _responseService = new ResponseService();
                _settingsService = new SettingsService();
                _forbiddenWordsService = new ForbiddenWordsService();
                
                System.Diagnostics.Debug.WriteLine("Создаем EmotionalReactionsService...");
                _emotionalReactionsService = new EmotionalReactionsService();
                System.Diagnostics.Debug.WriteLine($"EmotionalReactionsService создан: {_emotionalReactionsService != null}");
                
                _streamEventsService = new StreamEventsService();
                _obsService = new OBSService();
                _subtitlesService = new SubtitlesService(_obsService);
                
                System.Diagnostics.Debug.WriteLine("Все сервисы созданы успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА В КОНСТРУКТОРЕ ГЛАВНОГО ОКНА: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка инициализации главного окна: {ex.Message}", 
                               "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            
            _chatMessages = new ObservableCollection<ChatMessage>();
            _responses = new ObservableCollection<QuickResponse>();
            _random = new Random();
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
            _voiceSettings.VoiceProfiles.CollectionChanged += (s, e) => {
                ActiveProfilesListBox.Items.Refresh();
            };
            
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

            // После загрузки настроек:
            _twitchUserName = null;
            if (!string.IsNullOrEmpty(_appSettings.TwitchOAuthToken) && !string.IsNullOrEmpty(_appSettings.TwitchClientId))
            {
                // Проверяем токен и получаем ник
                _ = CheckTwitchAuthStatusAsync();
            }
            else
            {
                UpdateTwitchAuthUI();
            }
            StartTwitchUserCheckTimer();

            TwitchRewardComboBox.SelectionChanged += TwitchRewardComboBox_SelectionChanged;
            TwitchRefreshRewardsButton.Click += TwitchRefreshRewardsButton_Click;
            TTSModeRewardOnlyRadio.Checked += TTSModeRadio_Checked;
            TTSModeRolesOnlyRadio.Checked += TTSModeRadio_Checked;
            TTSModeBothRadio.Checked += TTSModeRadio_Checked;
            // Восстановить режим из настроек
            if (_appSettings.TTSMode == "reward")
                TTSModeRewardOnlyRadio.IsChecked = true;
            else if (_appSettings.TTSMode == "roles")
                TTSModeRolesOnlyRadio.IsChecked = true;
            else
                TTSModeBothRadio.IsChecked = true;
            // После авторизации — загрузить награды
            if (!string.IsNullOrEmpty(_appSettings.TwitchOAuthToken) && !string.IsNullOrEmpty(_appSettings.TwitchClientId))
                _ = LoadTwitchRewardsAsync();

            EventSoundsEnabledCheckBox.Checked += EventSoundsEnabledCheckBox_Checked;
            EventSoundsEnabledCheckBox.Unchecked += EventSoundsEnabledCheckBox_Unchecked;
            EventSoundsEnabledCheckBox.IsChecked = _appSettings.EventSoundsEnabled;
        }

        private async Task CheckTwitchAuthStatusAsync()
        {
            try
            {
                _twitchUserName = await GetTwitchUserNameAsync(_appSettings.TwitchOAuthToken, _appSettings.TwitchClientId);
            }
            catch
            {
                _twitchUserName = null;
                _appSettings.TwitchOAuthToken = string.Empty;
            }
            UpdateTwitchAuthUI();
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
            if (MaxChatMessagesTextBox != null)
            {
                MaxChatMessagesTextBox.Text = _appSettings.MaxChatMessages.ToString();
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
            var responses = _responseService.GetQuickResponses();
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
            if (VoiceOnlyForSubscribersCheckBox != null)
            {
                VoiceOnlyForSubscribersCheckBox.IsChecked = _appSettings.VoiceOnlyForSubscribers;
            }
            if (VoiceOnlyForVipsCheckBox != null)
            {
                VoiceOnlyForVipsCheckBox.IsChecked = _appSettings.VoiceOnlyForVips;
            }
            if (VoiceOnlyForModeratorsCheckBox != null)
            {
                VoiceOnlyForModeratorsCheckBox.IsChecked = _appSettings.VoiceOnlyForModerators;
            }
            if (ChatTriggerSymbolTextBox != null)
            {
                ChatTriggerSymbolTextBox.Text = _appSettings.ChatTriggerSymbol;
            }
            if (MaxChatMessagesTextBox != null)
            {
                MaxChatMessagesTextBox.Text = _appSettings.MaxChatMessages.ToString();
            }
            if (VolumeSlider != null && VolumeTextBlock != null)
            {
                var volume = _voiceSettings.VoiceProfiles.FirstOrDefault()?.Volume ?? 100;
                VolumeSlider.Value = volume;
                VolumeTextBlock.Text = $"{volume}%";
            }
            UpdateTwitchAuthUI();
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
                
                // Оптимизированное удаление старых сообщений - удаляем сразу все лишние
                var maxMessages = _appSettings?.MaxChatMessages ?? 100;
                if (_chatMessages.Count > maxMessages)
                {
                    var itemsToRemove = _chatMessages.Count - maxMessages;
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        _chatMessages.RemoveAt(0);
                    }
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
                // Проверяем настройки ограничений по ролям
                bool shouldVoice = true;
                
                // Если включены какие-либо ограничения, проверяем соответствие
                if (_appSettings.VoiceOnlyForSubscribers || _appSettings.VoiceOnlyForVips || _appSettings.VoiceOnlyForModerators)
                {
                    shouldVoice = false; // По умолчанию не озвучиваем
                    
                    // Проверяем каждую включенную группу
                    if (_appSettings.VoiceOnlyForSubscribers && message.IsSubscriber)
                    {
                        shouldVoice = true;
                    }
                    
                    if (_appSettings.VoiceOnlyForVips && message.IsVip)
                    {
                        shouldVoice = true;
                    }
                    
                    if (_appSettings.VoiceOnlyForModerators && message.IsModerator)
                    {
                        shouldVoice = true;
                    }
                }
                
                // Проверяем, содержит ли сообщение символ триггера
                if (shouldVoice && (string.IsNullOrEmpty(_appSettings.ChatTriggerSymbol) || 
                    message.Message.Contains(_appSettings.ChatTriggerSymbol)))
                {
                    // Проверяем запрещенные слова
                    if (!_forbiddenWordsService.ContainsForbiddenWords(message.Message))
                    {
                        // Озвучиваем сообщение чата
                        await _speechService.SpeakAsync(message.Message, message.Username, "chat");
                        
                        // Показываем субтитры для сообщения чата
                        await _subtitlesService.ShowSubtitleAsync(message.Message, message.Username);
                        
                        // Проверяем эмоциональные реакции
                        if (_emotionalReactionsService != null)
                        {
                            var emotionalReaction = _emotionalReactionsService.GetReactionForMessage(message.Message, message.Username);
                            if (emotionalReaction != null && emotionalReaction.Responses.Any())
                            {
                                // Небольшая задержка перед эмоциональной реакцией
                                await Task.Delay(500);
                                
                                var randomResponse = emotionalReaction.Responses[_random.Next(emotionalReaction.Responses.Count)];
                                await _speechService.SpeakAsync(randomResponse, "Стример", "emotional");
                                
                                // Показываем субтитры для эмоциональной реакции
                                await _subtitlesService.ShowEmotionalSubtitleAsync(randomResponse, message.Username);
                            }
                        }
                    }
                    else
                    {
                        // Логируем блокировку сообщения
                        System.Diagnostics.Debug.WriteLine($"Сообщение заблокировано из-за запрещенных слов: {message.Message}");
                    }
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
            var responses = _responseService.GetPersonalResponsesForMessage(message.Message);
            
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
                // Проверяем запрещенные слова
                if (!_forbiddenWordsService.ContainsForbiddenWords(data.Response))
                {
                    await _speechService.SpeakAsync(data.Response, data.Username, "quick");
                    
                    // Показываем субтитры для быстрого ответа
                    await _subtitlesService.ShowSubtitleAsync(data.Response, data.Username);
                }
                else
                {
                    MessageBox.Show("Ответ содержит запрещенные слова и не может быть озвучен!", 
                        "Запрещенные слова", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
                    
                    // Проверяем запрещенные слова
                    if (!_forbiddenWordsService.ContainsForbiddenWords(response))
                    {
                        await _speechService.SpeakAsync(response, "Стример", "quick");
                        
                        // Показываем субтитры для быстрого ответа
                        await _subtitlesService.ShowSubtitleAsync(response, "Стример");
                    }
                    else
                    {
                        MessageBox.Show("Быстрый ответ содержит запрещенные слова и не может быть озвучен!", 
                            "Запрещенные слова", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private async void SpeakMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                // Проверяем запрещенные слова
                if (!_forbiddenWordsService.ContainsForbiddenWords(message))
                {
                    await _speechService.SpeakAsync(message, null, "manual");
                    
                    // Показываем субтитры для ручного сообщения
                    await _subtitlesService.ShowSubtitleAsync(message, "Стример");
                }
                else
                {
                    MessageBox.Show("Сообщение содержит запрещенные слова и не может быть озвучено!", 
                        "Запрещенные слова", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = string.Empty;
        }

        private void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            _chatMessages.Clear();
            
            // Обновляем счетчик сообщений
            if (ChatCounterTextBlock != null)
            {
                ChatCounterTextBlock.Text = " (0)";
            }
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

        private void VoiceOnlyForSubscribersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForSubscribers = true;
                SaveAppSettings();
            }
        }

        private void VoiceOnlyForSubscribersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForSubscribers = false;
                SaveAppSettings();
            }
        }

        private void VoiceOnlyForVipsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForVips = true;
                SaveAppSettings();
            }
        }

        private void VoiceOnlyForVipsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForVips = false;
                SaveAppSettings();
            }
        }

        private void VoiceOnlyForModeratorsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForModerators = true;
                SaveAppSettings();
            }
        }

        private void VoiceOnlyForModeratorsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                _appSettings.VoiceOnlyForModerators = false;
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

        private void MaxChatMessagesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && _appSettings != null)
            {
                if (int.TryParse(textBox.Text, out int maxMessages) && maxMessages > 0)
                {
                    _appSettings.MaxChatMessages = maxMessages;
                    SaveAppSettings();
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && VolumeTextBlock != null)
            {
                var volume = (int)slider.Value;
                VolumeTextBlock.Text = $"{volume}%";
                
                // Обновляем громкость в голосовых настройках
                if (_voiceSettings != null)
                {
                    foreach (var profile in _voiceSettings.VoiceProfiles)
                    {
                        profile.Volume = volume;
                    }
                    UpdateVoiceSettings();
                }
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
            _speechService.UpdateSettings(_voiceSettings); // <-- обязательно обновить сервис озвучки
            
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

        private void OpenForbiddenWordsButton_Click(object sender, RoutedEventArgs e)
        {
            var forbiddenWordsWindow = new ForbiddenWordsWindow(_forbiddenWordsService);
            forbiddenWordsWindow.Owner = this;
            forbiddenWordsWindow.ShowDialog();
            
            // Обновляем список запрещенных слов
            _forbiddenWordsService.LoadForbiddenWords();
        }

        private void OpenEmotionalReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Подробная отладочная информация
                System.Diagnostics.Debug.WriteLine("=== ОТЛАДКА ОТКРЫТИЯ ОКНА ЭМОЦИЙ ===");
                System.Diagnostics.Debug.WriteLine($"Сервис в главном окне: {_emotionalReactionsService != null}");
                
                if (_emotionalReactionsService == null)
                {
                    System.Diagnostics.Debug.WriteLine("СЕРВИС NULL В ГЛАВНОМ ОКНЕ!");
                    MessageBox.Show("Ошибка: сервис эмоциональных реакций не инициализирован. Перезапустите приложение.", 
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Тип сервиса: {_emotionalReactionsService.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Глобальные настройки: {_emotionalReactionsService.GlobalEnabled}");
                System.Diagnostics.Debug.WriteLine($"Количество реакций: {_emotionalReactionsService.GetAllReactions().Count}");

                // Создаем окно с явной передачей сервиса
                var emotionalReactionsWindow = new EmotionalReactionsWindow(_emotionalReactionsService);
                System.Diagnostics.Debug.WriteLine("Окно создано успешно");
                
                emotionalReactionsWindow.Owner = this;
                emotionalReactionsWindow.ShowDialog();
                
                System.Diagnostics.Debug.WriteLine("Окно закрыто");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ИСКЛЮЧЕНИЕ: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка открытия настроек эмоциональных реакций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenStreamEventsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new StreamEventsWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private async void TestEmotionalReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_emotionalReactionsService == null)
                {
                    MessageBox.Show("Ошибка: сервис эмоциональных реакций не инициализирован.", 
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Тестируем эмоциональные реакции
                var testMessage = "хахаха это очень смешно!";
                var reaction = _emotionalReactionsService.GetReactionForMessage(testMessage);
                
                if (reaction != null && reaction.Responses.Any())
                {
                    var randomResponse = reaction.Responses[_random.Next(reaction.Responses.Count)];
                    await _speechService.SpeakAsync(randomResponse, "Стример", "emotional");
                    MessageBox.Show($"Сработала эмоциональная реакция: {reaction.Name}\nОтвет: {randomResponse}", 
                        "Тест эмоций", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Эмоциональная реакция не найдена. Проверьте настройки эмоциональных реакций.", 
                        "Тест эмоций", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования эмоциональных реакций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestStreamEventsButton_Click(object sender, RoutedEventArgs e)
        {
            // Тестируем события стрима (подписка)
            var subscriptionEvent = _streamEventsService.GetEventForSubscription(EventType.Subscription, 1);
            
            if (subscriptionEvent != null && subscriptionEvent.Responses.Any())
            {
                var randomResponse = subscriptionEvent.Responses[_random.Next(subscriptionEvent.Responses.Count)];
                // Заменяем {username} на тестовое имя
                randomResponse = randomResponse.Replace("{username}", "ТестовыйПодписчик");
                await _speechService.SpeakAsync(randomResponse, "Стример", "subscription");
                MessageBox.Show($"Сработало событие стрима: {subscriptionEvent.Name}\nОтвет: {randomResponse}", 
                    "Тест событий", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Событие стрима не найдено. Проверьте настройки событий стрима.", 
                    "Тест событий", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void TestDonationButton_Click(object sender, RoutedEventArgs e)
        {
            // Тестируем события стрима (донат)
            var donationEvent = _streamEventsService.GetEventForDonation(10.0m, "ТестовыйДонатер");
            
            if (donationEvent != null && donationEvent.Responses.Any())
            {
                var randomResponse = donationEvent.Responses[_random.Next(donationEvent.Responses.Count)];
                // Заменяем {username} на тестовое имя
                randomResponse = randomResponse.Replace("{username}", "ТестовыйДонатер");
                await _speechService.SpeakAsync(randomResponse, "Стример", "donation");
                MessageBox.Show($"Сработало событие доната: {donationEvent.Name}\nОтвет: {randomResponse}", 
                    "Тест доната", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Событие доната не найдено. Проверьте настройки событий стрима.", 
                    "Тест доната", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void OBSIntegrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obsWindow = new OBSIntegrationWindow(_obsService, _subtitlesService);
                obsWindow.Owner = this;
                obsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна OBS интеграции: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubtitlesSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obsWindow = new OBSIntegrationWindow(_obsService, _subtitlesService);
                obsWindow.Owner = this;
                obsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия настроек субтитров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Сохраняем настройки приложения
            SaveAppSettings();
            
            // Отключаемся от чата
            if (_twitchService.IsConnected)
            {
                _twitchService.DisconnectAsync().Wait();
            }
            
            // Освобождаем ресурсы
            _speechService?.Dispose();
            _obsService?.Dispose();
            _subtitlesService?.Dispose();
            _twitchUserCheckTimer?.Stop();
            _twitchUserCheckTimer?.Dispose();
            
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
            
            // Сохраняем настройки озвучки чата
            _appSettings.EnableChatVoice = EnableChatVoiceCheckBox.IsChecked == true;
            _appSettings.VoiceOnlyForSubscribers = VoiceOnlyForSubscribersCheckBox.IsChecked == true;
            _appSettings.VoiceOnlyForVips = VoiceOnlyForVipsCheckBox.IsChecked == true;
            _appSettings.VoiceOnlyForModerators = VoiceOnlyForModeratorsCheckBox.IsChecked == true;
            _appSettings.ChatTriggerSymbol = ChatTriggerSymbolTextBox.Text;
            _appSettings.MaxChatMessages = int.TryParse(MaxChatMessagesTextBox.Text, out int maxMessages) ? maxMessages : 100;
            _appSettings.TTSMode = TTSModeRewardOnlyRadio.IsChecked == true ? "reward" : TTSModeRolesOnlyRadio.IsChecked == true ? "roles" : "both";
            _appSettings.EventSoundsEnabled = EventSoundsEnabledCheckBox.IsChecked == true;

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

        private void InitTwitchPubSub()
        {
            if (string.IsNullOrWhiteSpace(_appSettings.TwitchOAuthToken) ||
                string.IsNullOrWhiteSpace(_appSettings.TwitchChannelId) ||
                string.IsNullOrWhiteSpace(_appSettings.TwitchRewardId))
                return;
            _pubSub = new TwitchPubSub();
            _pubSub.OnPubSubServiceConnected += (s, e) =>
            {
                _pubSub.ListenToChannelPoints(_appSettings.TwitchChannelId);
                _pubSub.SendTopics(_appSettings.TwitchOAuthToken.StartsWith("oauth:") ? _appSettings.TwitchOAuthToken : $"oauth:{_appSettings.TwitchOAuthToken}");
            };
            _pubSub.OnRewardRedeemed += OnRewardRedeemed;
            _pubSub.Connect();
        }
        private async void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            // Попробуй так:
            var reward = e.GetType().GetProperty("RewardRedeemed")?.GetValue(e, null);
            if (reward != null)
            {
                var rewardId = reward.GetType().GetProperty("RewardId")?.GetValue(reward, null)?.ToString();
                var displayName = reward.GetType().GetProperty("DisplayName")?.GetValue(reward, null)?.ToString();
                var userInput = reward.GetType().GetProperty("UserInput")?.GetValue(reward, null)?.ToString();

                System.Diagnostics.Debug.WriteLine($"RewardId: {rewardId}, DisplayName: {displayName}, UserInput: {userInput}");

                if (rewardId == _appSettings.TwitchRewardId)
                {
                    if (!_forbiddenWordsService.ContainsForbiddenWords(userInput))
                    {
                        await _speechService.SpeakAsync(userInput, displayName, "chat");
                        await _subtitlesService.ShowSubtitleAsync(userInput, displayName);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("RewardRedeemed property not found on OnRewardRedeemedArgs!");
            }
        }

        private async void TwitchRefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckTwitchAuthStatusAsync();
        }

        private void ShowTwitchDeviceFlow(string verificationUri, string userCode)
        {
            if (TwitchDeviceFlowPanel != null)
                TwitchDeviceFlowPanel.Visibility = Visibility.Visible;
            if (TwitchDeviceFlowLink != null)
            {
                TwitchDeviceFlowLink.NavigateUri = new Uri(verificationUri);
                TwitchDeviceFlowLink.Inlines.Clear();
                TwitchDeviceFlowLink.Inlines.Add(verificationUri);
            }
            if (TwitchDeviceFlowCodeBox != null)
                TwitchDeviceFlowCodeBox.Text = userCode;
        }
        private void HideTwitchDeviceFlow()
        {
            if (TwitchDeviceFlowPanel != null)
                TwitchDeviceFlowPanel.Visibility = Visibility.Collapsed;
        }

        private async void TwitchLoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? clientId = null;
                string? clientSecret = null;
                if (TwitchAdvancedModeCheckBox != null && TwitchAdvancedModeCheckBox.IsChecked == true)
                {
                    clientId = TwitchClientIdTextBox?.Text;
                    clientSecret = TwitchClientSecretBox?.Password;
                }
                var oauth = new TwitchOAuthService(clientId, clientSecret, "channel:read:redemptions user:read:chat user:read:email");
                var deviceFlowInfo = await oauth.RequestDeviceCodeAsync();
                string url = deviceFlowInfo.VerificationUri;
                if (!string.IsNullOrEmpty(deviceFlowInfo.UserCode))
                {
                    url += (url.Contains("?") ? "&" : "?") + "user_code=" + Uri.EscapeDataString(deviceFlowInfo.UserCode);
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                ShowTwitchDeviceFlow(deviceFlowInfo.VerificationUri, deviceFlowInfo.UserCode);
                var token = await oauth.PollDeviceTokenAsync(deviceFlowInfo.DeviceCode, deviceFlowInfo.Interval);
                HideTwitchDeviceFlow();
                if (!string.IsNullOrEmpty(token))
                {
                    _appSettings.TwitchOAuthToken = token;
                    _appSettings.TwitchClientId = clientId ?? TwitchOAuthService.DefaultClientId;
                    SaveAppSettings();
                    await CheckTwitchAuthStatusAsync();
                    UpdateTwitchAuthUI(); // Явно обновить статус
                    System.Windows.MessageBox.Show($"Успешная авторизация через Twitch!{(_twitchUserName != null ? "\nПользователь: " + _twitchUserName : "")}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateTwitchAuthUI(); // На всякий случай
                    System.Windows.MessageBox.Show("Не удалось получить токен Twitch.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                HideTwitchDeviceFlow();
                UpdateTwitchAuthUI();
                System.Windows.MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string?> GetTwitchUserNameAsync(string accessToken, string clientId)
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            http.DefaultRequestHeaders.Add("Client-Id", clientId);
            var resp = await http.GetAsync("https://api.twitch.tv/helix/users");
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                return data[0].GetProperty("display_name").GetString();
            return null;
        }

        private async Task LoadTwitchRewardsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_appSettings.TwitchOAuthToken) || string.IsNullOrEmpty(_appSettings.TwitchClientId))
                {
                    MessageBox.Show("Сначала выполните авторизацию в Twitch!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Получаем broadcaster_id (user_id)
                string? userId = await GetTwitchUserIdAsync(_appSettings.TwitchOAuthToken, _appSettings.TwitchClientId);
                if (string.IsNullOrEmpty(userId))
                {
                    MessageBox.Show("Не удалось получить ID пользователя Twitch.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_appSettings.TwitchOAuthToken}");
                http.DefaultRequestHeaders.Add("Client-Id", _appSettings.TwitchClientId);
                var resp = await http.GetAsync($"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={userId}");
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                {
                    MessageBox.Show("У вас нет созданных наград Channel Points. Создайте награду на панели управления Twitch.", "Нет наград", MessageBoxButton.OK, MessageBoxImage.Information);
                    TwitchRewardComboBox.ItemsSource = null;
                    return;
                }
                var rewards = new List<(string Id, string Title, string Prompt)>();
                foreach (var reward in data.EnumerateArray())
                {
                    var id = reward.GetProperty("id").GetString() ?? "";
                    var title = reward.GetProperty("title").GetString() ?? "";
                    var prompt = reward.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "";
                    rewards.Add((id, title, prompt));
                }
                TwitchRewardComboBox.ItemsSource = rewards;
                TwitchRewardComboBox.DisplayMemberPath = "Title";
                TwitchRewardComboBox.SelectedValuePath = "Id";
                // Восстановить выбор из настроек
                if (!string.IsNullOrEmpty(_appSettings.TwitchRewardId))
                    TwitchRewardComboBox.SelectedValue = _appSettings.TwitchRewardId;
                else
                    TwitchRewardComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки наград Twitch: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string?> GetTwitchUserIdAsync(string accessToken, string clientId)
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            http.DefaultRequestHeaders.Add("Client-Id", clientId);
            var resp = await http.GetAsync("https://api.twitch.tv/helix/users");
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                return data[0].GetProperty("id").GetString();
            return null;
        }

        private async void TwitchRefreshRewardsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTwitchRewardsAsync();
        }

        private void TwitchRewardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TwitchRewardComboBox.SelectedValue is string rewardId)
            {
                _appSettings.TwitchRewardId = rewardId;
                SaveAppSettings();
            }
        }

        private void TTSModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TTSModeRewardOnlyRadio.IsChecked == true)
                _appSettings.TTSMode = "reward";
            else if (TTSModeRolesOnlyRadio.IsChecked == true)
                _appSettings.TTSMode = "roles";
            else if (TTSModeBothRadio.IsChecked == true)
                _appSettings.TTSMode = "both";
            SaveAppSettings();
        }

        private void TwitchLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _appSettings.TwitchOAuthToken = string.Empty;
            _appSettings.TwitchClientId = string.Empty;
            _twitchUserName = null;
            SaveAppSettings();
            UpdateTwitchAuthUI();
            System.Windows.MessageBox.Show("Вы вышли из Twitch.", "Twitch", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateTwitchAuthUI()
        {
            bool isAuth = !string.IsNullOrEmpty(_appSettings.TwitchOAuthToken);
            if (TwitchAuthStatusTextBlock != null)
            {
                TwitchAuthStatusTextBlock.Text = isAuth ? $"Статус авторизации: Авторизован{(_twitchUserName != null ? " как " + _twitchUserName : "")}" : "Статус авторизации: Не авторизован";
            }
            if (TwitchLoginButton != null)
                TwitchLoginButton.IsEnabled = !isAuth;
            if (TwitchLogoutButton != null)
                TwitchLogoutButton.Visibility = isAuth ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            // Здесь можно заблокировать/разблокировать другие поля, если они есть
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        // Вспомогательный метод для ввода client secret
        private Task<string?> ShowInputDialogAsync(string prompt)
        {
            var tcs = new TaskCompletionSource<string?>();
            var inputWindow = new Window
            {
                Title = prompt,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = this
            };
            var stack = new StackPanel { Margin = new Thickness(16) };
            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
            var okButton = new Button { Content = "OK", Width = 80, IsDefault = true };
            okButton.Click += (s, e) => { inputWindow.DialogResult = true; inputWindow.Close(); };
            stack.Children.Add(textBox);
            stack.Children.Add(okButton);
            inputWindow.Content = stack;
            inputWindow.ShowDialog();
            tcs.SetResult(textBox.Text);
            return tcs.Task;
        }

        private void StartTwitchUserCheckTimer()
        {
            _twitchUserCheckTimer = new System.Timers.Timer(5 * 60 * 1000); // 5 минут
            _twitchUserCheckTimer.Elapsed += async (s, e) =>
            {
                if (!string.IsNullOrEmpty(_appSettings.TwitchOAuthToken) && !string.IsNullOrEmpty(_appSettings.TwitchClientId))
                {
                    await Dispatcher.InvokeAsync(async () => await CheckTwitchAuthStatusAsync());
                }
            };
            _twitchUserCheckTimer.AutoReset = true;
            _twitchUserCheckTimer.Start();
        }

        private void TwitchAdvancedModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (TwitchAdvancedPanel != null)
                TwitchAdvancedPanel.Visibility = System.Windows.Visibility.Visible;
        }
        private void TwitchAdvancedModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (TwitchAdvancedPanel != null)
                TwitchAdvancedPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void EventSoundsEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _appSettings.EventSoundsEnabled = true;
            SaveAppSettings();
        }
        private void EventSoundsEnabledCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _appSettings.EventSoundsEnabled = false;
            SaveAppSettings();
        }
    }
} 