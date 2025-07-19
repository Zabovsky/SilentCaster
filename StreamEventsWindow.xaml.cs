using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class StreamEventsWindow : Window
    {
        private readonly StreamEventsService _streamEventsService;
        private readonly ObservableCollection<StreamEvent> _streamEvents;
        private StreamEvent? _currentEvent;
        private bool _isEditing = false;

        public StreamEventsWindow()
        {
            InitializeComponent();
            
            _streamEventsService = new StreamEventsService();
            _streamEvents = new ObservableCollection<StreamEvent>(_streamEventsService.GetAllEvents());
            
            EventsListBox.ItemsSource = _streamEvents;
            
            // Заполняем комбобоксы
            EventTypeComboBox.ItemsSource = Enum.GetValues(typeof(EventType));
            EventTypeComboBox.SelectedIndex = 0;
            
            EventTypeFilterComboBox.ItemsSource = new[] { "Все типы" }.Concat(Enum.GetValues(typeof(EventType)).Cast<EventType>().Select(e => e.ToString()));
            EventTypeFilterComboBox.SelectedIndex = 0;
            
            // Привязываем события слайдеров
            EventPrioritySlider.ValueChanged += (s, e) => EventPriorityValueText.Text = ((int)e.NewValue).ToString();
            EventUsageChanceSlider.ValueChanged += (s, e) => EventUsageChanceValueText.Text = $"{(int)e.NewValue}%";
            
            // Устанавливаем начальные значения
            EventPriorityValueText.Text = "1";
            EventUsageChanceValueText.Text = "100%";
        }

        private void EventTypeFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EventTypeFilterComboBox.SelectedItem is string selectedFilter)
            {
                if (selectedFilter == "Все типы")
                {
                    EventsListBox.ItemsSource = _streamEvents;
                }
                else if (Enum.TryParse<EventType>(selectedFilter, out var eventType))
                {
                    var filteredEvents = _streamEvents.Where(e => e.Type == eventType).ToList();
                    EventsListBox.ItemsSource = filteredEvents;
                }
            }
        }

        private void EventsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EventsListBox.SelectedItem is StreamEvent streamEvent)
            {
                LoadEventDetails(streamEvent);
                EventDetailsPanel.IsEnabled = true;
                _currentEvent = streamEvent;
                _isEditing = true;
            }
            else
            {
                ClearEventDetails();
                EventDetailsPanel.IsEnabled = false;
                _currentEvent = null;
                _isEditing = false;
            }
        }

        private void LoadEventDetails(StreamEvent streamEvent)
        {
            EventNameTextBox.Text = streamEvent.Name;
            EventDescriptionTextBox.Text = streamEvent.Description;
            EventTypeComboBox.SelectedItem = streamEvent.Type;
            EventPrioritySlider.Value = streamEvent.Priority;
            EventUsageChanceSlider.Value = streamEvent.UsageChance;
            EventIsEnabledCheckBox.IsChecked = streamEvent.IsEnabled;
            
            // Загружаем специальные настройки
            MinAmountTextBox.Text = streamEvent.MinAmount?.ToString() ?? string.Empty;
            MaxAmountTextBox.Text = streamEvent.MaxAmount?.ToString() ?? string.Empty;
            SubscriberMonthsTextBox.Text = streamEvent.SubscriberMonths?.ToString() ?? string.Empty;
            RaidViewersTextBox.Text = streamEvent.RaidViewers?.ToString() ?? string.Empty;
            
            EventResponsesTextBox.Text = string.Join(Environment.NewLine, streamEvent.Responses);
            
            // Показываем соответствующие панели настроек
            UpdateSettingsPanelsVisibility(streamEvent.Type);
        }

        private void ClearEventDetails()
        {
            EventNameTextBox.Text = string.Empty;
            EventDescriptionTextBox.Text = string.Empty;
            EventTypeComboBox.SelectedIndex = 0;
            EventPrioritySlider.Value = 1;
            EventUsageChanceSlider.Value = 100;
            EventIsEnabledCheckBox.IsChecked = true;
            
            MinAmountTextBox.Text = string.Empty;
            MaxAmountTextBox.Text = string.Empty;
            SubscriberMonthsTextBox.Text = string.Empty;
            RaidViewersTextBox.Text = string.Empty;
            EventResponsesTextBox.Text = string.Empty;
            
            // Скрываем все панели настроек
            DonationSettingsPanel.Visibility = Visibility.Collapsed;
            SubscriptionSettingsPanel.Visibility = Visibility.Collapsed;
            RaidSettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateSettingsPanelsVisibility(EventType eventType)
        {
            // Скрываем все панели
            DonationSettingsPanel.Visibility = Visibility.Collapsed;
            SubscriptionSettingsPanel.Visibility = Visibility.Collapsed;
            RaidSettingsPanel.Visibility = Visibility.Collapsed;
            
            // Показываем соответствующую панель
            switch (eventType)
            {
                case EventType.Donation:
                    DonationSettingsPanel.Visibility = Visibility.Visible;
                    break;
                case EventType.Subscription:
                case EventType.Resubscription:
                case EventType.GiftSubscription:
                    SubscriptionSettingsPanel.Visibility = Visibility.Visible;
                    break;
                case EventType.Raid:
                    RaidSettingsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void EventTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EventTypeComboBox.SelectedItem is EventType eventType)
            {
                UpdateSettingsPanelsVisibility(eventType);
            }
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEventDetails();
            EventDetailsPanel.IsEnabled = true;
            _currentEvent = new StreamEvent();
            _isEditing = false;
            
            // Снимаем выделение с списка
            EventsListBox.SelectedItem = null;
        }

        private void EditEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventsListBox.SelectedItem is StreamEvent streamEvent)
            {
                LoadEventDetails(streamEvent);
                EventDetailsPanel.IsEnabled = true;
                _currentEvent = streamEvent;
                _isEditing = true;
            }
            else
            {
                MessageBox.Show("Выберите событие для редактирования.", "Предупреждение", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventsListBox.SelectedItem is StreamEvent streamEvent)
            {
                var result = MessageBox.Show($"Удалить событие '{streamEvent.Name}'?", "Подтверждение", 
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _streamEventsService.RemoveEvent(streamEvent.Id);
                    _streamEvents.Remove(streamEvent);
                    
                    if (_currentEvent?.Id == streamEvent.Id)
                    {
                        ClearEventDetails();
                        EventDetailsPanel.IsEnabled = false;
                        _currentEvent = null;
                        _isEditing = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите событие для удаления.", "Предупреждение", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                MessageBox.Show("Введите название события.", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EventResponsesTextBox.Text))
            {
                MessageBox.Show("Введите хотя бы один ответ.", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var streamEvent = _currentEvent ?? new StreamEvent();
                
                streamEvent.Name = EventNameTextBox.Text.Trim();
                streamEvent.Description = EventDescriptionTextBox.Text.Trim();
                streamEvent.Type = (EventType)EventTypeComboBox.SelectedItem;
                streamEvent.Priority = (int)EventPrioritySlider.Value;
                streamEvent.UsageChance = EventUsageChanceSlider.Value;
                streamEvent.IsEnabled = EventIsEnabledCheckBox.IsChecked ?? true;
                
                // Парсим специальные настройки
                if (decimal.TryParse(MinAmountTextBox.Text, out var minAmount))
                    streamEvent.MinAmount = minAmount;
                else
                    streamEvent.MinAmount = null;
                
                if (decimal.TryParse(MaxAmountTextBox.Text, out var maxAmount))
                    streamEvent.MaxAmount = maxAmount;
                else
                    streamEvent.MaxAmount = null;
                
                if (int.TryParse(SubscriberMonthsTextBox.Text, out var subscriberMonths))
                    streamEvent.SubscriberMonths = subscriberMonths;
                else
                    streamEvent.SubscriberMonths = null;
                
                if (int.TryParse(RaidViewersTextBox.Text, out var raidViewers))
                    streamEvent.RaidViewers = raidViewers;
                else
                    streamEvent.RaidViewers = null;
                
                // Парсим ответы
                streamEvent.Responses = EventResponsesTextBox.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();

                if (!_isEditing)
                {
                    // Добавляем новое событие
                    _streamEventsService.AddEvent(streamEvent);
                    _streamEvents.Add(streamEvent);
                }
                else
                {
                    // Обновляем существующее
                    _streamEventsService.UpdateEvent(streamEvent);
                    var index = _streamEvents.IndexOf(_currentEvent!);
                    if (index >= 0)
                    {
                        _streamEvents[index] = streamEvent;
                    }
                }

                MessageBox.Show("Событие сохранено успешно!", "Успех", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                
                ClearEventDetails();
                EventDetailsPanel.IsEnabled = false;
                _currentEvent = null;
                _isEditing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing && _currentEvent != null)
            {
                LoadEventDetails(_currentEvent);
            }
            else
            {
                ClearEventDetails();
                EventDetailsPanel.IsEnabled = false;
                _currentEvent = null;
                _isEditing = false;
            }
        }

        private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сбросить все события стрима к настройкам по умолчанию? " +
                                        "Это удалит все ваши настройки.", "Подтверждение", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем файл настроек, чтобы загрузились настройки по умолчанию
                    var filePath = "StreamEvents.json";
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    
                    // Пересоздаем сервис
                    _streamEventsService.GetType().GetMethod("LoadStreamEvents", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(_streamEventsService, null);
                    
                    // Обновляем список
                    _streamEvents.Clear();
                    foreach (var streamEvent in _streamEventsService.GetAllEvents())
                    {
                        _streamEvents.Add(streamEvent);
                    }
                    
                    ClearEventDetails();
                    EventDetailsPanel.IsEnabled = false;
                    _currentEvent = null;
                    _isEditing = false;
                    
                    MessageBox.Show("Настройки сброшены к значениям по умолчанию!", "Успех", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сбросе настроек: {ex.Message}", "Ошибка", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
    }
} 