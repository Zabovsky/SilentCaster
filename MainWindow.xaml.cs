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