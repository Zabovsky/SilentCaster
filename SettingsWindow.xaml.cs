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
        private readonly ObservableCollection<QuickResponse> _responses;
        private QuickResponse? _currentResponse;

        public SettingsWindow(ResponseService responseService, SpeechService speechService)
        {
            InitializeComponent();
            
            _responseService = responseService;
            _speechService = speechService;
            _responses = new ObservableCollection<QuickResponse>();
            
            ResponsesListBox.ItemsSource = _responses;
            LoadResponses();
            UpdateStatistics();
        }

        private void LoadResponses()
        {
            _responses.Clear();
            var responses = _responseService.GetAllResponses();
            foreach (var response in responses)
            {
                // Инициализируем новые поля для старых ответов
                if (string.IsNullOrEmpty(response.Response) && response.Responses != null && response.Responses.Count > 0)
                {
                    response.Response = string.Join(Environment.NewLine, response.Responses);
                }
                
                // Устанавливаем значения по умолчанию для новых полей
                if (string.IsNullOrEmpty(response.Category))
                    response.Category = "Общие";
                if (response.Priority == 0)
                    response.Priority = 1;
                if (!response.IsEnabled)
                    response.IsEnabled = true;
                if (!response.UseForChatMessages)
                    response.UseForChatMessages = true;
                if (!response.UseForManualMessages)
                    response.UseForManualMessages = true;
                if (!response.UseForQuickResponses)
                    response.UseForQuickResponses = true;
                if (response.UsageChance == 0)
                    response.UsageChance = 100;
                if (response.Delay == 0)
                    response.Delay = 0;
                
                _responses.Add(response);
            }
            
            // Обновляем статистику после загрузки
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            TotalResponsesTextBlock.Text = _responses.Count.ToString();
            var activeCount = _responses.Count(r => r.IsEnabled);
            ActiveResponsesTextBlock.Text = activeCount.ToString();
        }

        private void ResponsesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResponsesListBox.SelectedItem is QuickResponse selectedResponse)
            {
                LoadResponse(selectedResponse);
                ShowResponseSettings();
            }
            else
            {
                HideResponseSettings();
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
            ResponseSettingsBorder.Visibility = Visibility.Visible;
            NoResponseBorder.Visibility = Visibility.Collapsed;
        }

        private void HideResponseSettings()
        {
            ResponseSettingsBorder.Visibility = Visibility.Collapsed;
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
                Delay = 0
            };

            _responses.Add(newResponse);
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
                    _responses.Remove(selectedResponse);
                    HideResponseSettings();
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
                await _speechService.SpeakAsync(_currentResponse.Response, "Тест");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Сохраняем все ответы
                _responseService.ClearResponses();
                foreach (var response in _responses)
                {
                    _responseService.AddResponse(response);
                }
                
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