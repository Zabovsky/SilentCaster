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
    public partial class SettingsWindow : Window
    {
        private readonly ResponseService _responseService;
        private readonly SpeechService _speechService;
        private readonly ForbiddenWordsService _forbiddenWordsService;
        private readonly ObservableCollection<QuickResponse> _quickResponses = new();
        private readonly ObservableCollection<QuickResponse> _personalResponses = new();
        private QuickResponse? _currentResponse;

        public SettingsWindow(ResponseService responseService, SpeechService speechService)
        {
            InitializeComponent();
            
            _responseService = responseService;
            _speechService = speechService;
            _forbiddenWordsService = new ForbiddenWordsService();
            _quickResponses = new ObservableCollection<QuickResponse>(_responseService.GetQuickResponses());
            _personalResponses = new ObservableCollection<QuickResponse>(_responseService.GetPersonalResponses());
            QuickResponsesListBox.ItemsSource = _quickResponses;
            ResponsesListBox.ItemsSource = _personalResponses;
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            TotalResponsesTextBlock.Text = (_quickResponses.Count + _personalResponses.Count).ToString();
            var activeCount = _quickResponses.Count(r => r.IsEnabled) + _personalResponses.Count(r => r.IsEnabled);
            ActiveResponsesTextBlock.Text = activeCount.ToString();
        }

        private void ResponsesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResponsesListBox.SelectedItem is QuickResponse selectedResponse)
            {
                LoadResponse(selectedResponse);
                // Переключаемся на первую вкладку (настройки ответа)
                ResponseSettingsTabControl.SelectedIndex = 0;
            }
        }

        private void QuickResponsesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuickResponsesListBox.SelectedItem is QuickResponse selectedQuick)
            {
                LoadResponse(selectedQuick);
                // Переключаемся на первую вкладку (настройки ответа)
                ResponseSettingsTabControl.SelectedIndex = 0;
            }
        }

        private void LoadResponse(QuickResponse response)
        {
            _currentResponse = response;
            
            // Убеждаемся, что триггер не пустой
            if (string.IsNullOrEmpty(response.Trigger))
            {
                response.Trigger = "новый_триггер";
            }
            
            TriggerTextBox.Text = response.Trigger;
            TriggerTextBox.Focus(); // Фокусируемся на поле для лучшей видимости
            
            // Обрабатываем как старые, так и новые форматы ответов
            if (!string.IsNullOrEmpty(response.Response))
            {
                ResponseTextBox.Text = response.Response;
            }
            else if (response.Responses != null && response.Responses.Count > 0)
            {
                ResponseTextBox.Text = string.Join(Environment.NewLine, response.Responses);
            }
            else
            {
                ResponseTextBox.Text = string.Empty;
            }
            
            // Устанавливаем категорию
            bool categoryFound = false;
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Content.ToString() == response.Category)
                {
                    CategoryComboBox.SelectedItem = item;
                    categoryFound = true;
                    break;
                }
            }
            
            // Если категория не найдена, устанавливаем "Общие" по умолчанию
            if (!categoryFound)
            {
                foreach (ComboBoxItem item in CategoryComboBox.Items)
                {
                    if (item.Content.ToString() == "Общие")
                    {
                        CategoryComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            
            PrioritySlider.Value = response.Priority;
            IsEnabledCheckBox.IsChecked = response.IsEnabled;
            
            // Настройки использования
            UseForChatMessagesCheckBox.IsChecked = response.UseForChatMessages;
            UseForManualMessagesCheckBox.IsChecked = response.UseForManualMessages;
            UseForQuickResponsesCheckBox.IsChecked = response.UseForQuickResponses;
            
            UsageChanceSlider.Value = response.UsageChance;
            DelaySlider.Value = response.Delay;
            
            // Обновляем текстовые блоки
            PriorityTextBlock.Text = ((int)response.Priority).ToString();
            UsageChanceTextBlock.Text = $"{(int)response.UsageChance}%";
            DelayTextBlock.Text = $"{(int)response.Delay} сек";
        }

        private void ShowResponseSettings()
        {
            ResponseSettingsTabControl.Visibility = Visibility.Visible;
            NoResponseBorder.Visibility = Visibility.Collapsed;
        }

        private void HideResponseSettings()
        {
            ResponseSettingsTabControl.Visibility = Visibility.Collapsed;
            NoResponseBorder.Visibility = Visibility.Visible;
        }

        private void AddResponseButton_Click(object sender, RoutedEventArgs e)
        {
            var newResponse = new QuickResponse
            {
                Trigger = "новый_триггер",
                Response = "Новый ответ",
                Responses = new List<string> { "Новый ответ" },
                Category = "Общие",
                Priority = 1,
                IsEnabled = true,
                UseForChatMessages = true,
                UseForManualMessages = true,
                UseForQuickResponses = true,
                UsageChance = 100,
                Delay = 0,
                IsQuickResponse = false // по умолчанию персональный
            };
            _personalResponses.Add(newResponse);
            ResponsesListBox.SelectedItem = newResponse;
            UpdateStatistics();
        }

        private void RemoveResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResponsesListBox.SelectedItem is QuickResponse selectedResponse)
            {
                var result = MessageBox.Show(
                    $"Удалить ответ '{selectedResponse.Trigger}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    _personalResponses.Remove(selectedResponse);
                    UpdateStatistics();
                }
            }
        }

        private void AddQuickResponseButton_Click(object sender, RoutedEventArgs e)
        {
            var newQuick = new QuickResponse
            {
                Trigger = "новый_триггер",
                Response = "Быстрый ответ",
                Responses = new List<string> { "Быстрый ответ" },
                Category = "Быстрый",
                Priority = 1,
                IsEnabled = true,
                UseForChatMessages = false,
                UseForManualMessages = false,
                UseForQuickResponses = true,
                UsageChance = 100,
                Delay = 0,
                IsQuickResponse = true
            };
            _quickResponses.Add(newQuick);
            QuickResponsesListBox.SelectedItem = newQuick;
            UpdateStatistics();
        }

        private void RemoveQuickResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuickResponsesListBox.SelectedItem is QuickResponse selectedQuick)
            {
                var result = MessageBox.Show(
                    $"Удалить быстрый ответ '{selectedQuick.Trigger}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    _quickResponses.Remove(selectedQuick);
                    UpdateStatistics();
                }
            }
        }

        private void SaveResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResponse == null)
            {
                MessageBox.Show("Выберите ответ для редактирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(TriggerTextBox.Text))
            {
                MessageBox.Show("Введите триггер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("Введите ответ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Обновляем ответ
            _currentResponse.Trigger = TriggerTextBox.Text.Trim();
            _currentResponse.Response = ResponseTextBox.Text.Trim();
            
            // Также обновляем старый формат для совместимости
            var responseLines = ResponseTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();
            _currentResponse.Responses = responseLines;
            
            if (CategoryComboBox.SelectedItem is ComboBoxItem selectedCategory)
            {
                _currentResponse.Category = selectedCategory.Content.ToString() ?? "Общие";
            }
            
            _currentResponse.Priority = (int)PrioritySlider.Value;
            _currentResponse.IsEnabled = IsEnabledCheckBox.IsChecked ?? true;
            
            // Настройки использования
            _currentResponse.UseForChatMessages = UseForChatMessagesCheckBox.IsChecked ?? true;
            _currentResponse.UseForManualMessages = UseForManualMessagesCheckBox.IsChecked ?? true;
            _currentResponse.UseForQuickResponses = UseForQuickResponsesCheckBox.IsChecked ?? true;
            
            _currentResponse.UsageChance = UsageChanceSlider.Value;
            _currentResponse.Delay = DelaySlider.Value;
            
            // Обновляем отображение в списке
            ResponsesListBox.Items.Refresh();
            QuickResponsesListBox.Items.Refresh();
            UpdateStatistics();
            
            MessageBox.Show("Ответ сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void TestResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResponse == null)
            {
                MessageBox.Show("Выберите ответ для тестирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Проверяем запрещенные слова
                if (!_forbiddenWordsService.ContainsForbiddenWords(_currentResponse.Response))
                {
                    await _speechService.SpeakAsync(_currentResponse.Response, "Тест");
                }
                else
                {
                    MessageBox.Show("Ответ содержит запрещенные слова и не может быть озвучен!", 
                        "Запрещенные слова", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestQuickResponse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuickResponse response)
            {
                try
                {
                    var responseText = response.Response;
                    if (string.IsNullOrEmpty(responseText) && response.Responses != null && response.Responses.Count > 0)
                    {
                        responseText = response.Responses.First();
                    }
                    
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        // Проверяем запрещенные слова
                        if (!_forbiddenWordsService.ContainsForbiddenWords(responseText))
                        {
                            await _speechService.SpeakAsync(responseText, null, "test");
                        }
                        else
                        {
                            MessageBox.Show("Быстрый ответ содержит запрещенные слова и не может быть озвучен!", 
                                "Запрещенные слова", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка тестирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrioritySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PriorityTextBlock != null)
            {
                PriorityTextBlock.Text = ((int)e.NewValue).ToString();
            }
        }

        private void UsageChanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (UsageChanceTextBlock != null)
            {
                UsageChanceTextBlock.Text = $"{(int)e.NewValue}%";
            }
        }

        private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DelayTextBlock != null)
            {
                DelayTextBlock.Text = $"{(int)e.NewValue} сек";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем все ответы (быстрые и персональные) в один файл
                var allResponses = _quickResponses.Concat(_personalResponses).ToList();
                _responseService.UpdateAllResponses(allResponses);
                
                MessageBox.Show("Все ответы сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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