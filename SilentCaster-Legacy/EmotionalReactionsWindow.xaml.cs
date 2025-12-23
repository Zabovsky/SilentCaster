using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class EmotionalReactionsWindow : Window
    {
        private readonly EmotionalReactionsService _emotionalReactionsService;
        private readonly ObservableCollection<EmotionalReaction> _emotionalReactions;
        private EmotionalReaction? _currentReaction;
        private bool _isEditing = false;

        public EmotionalReactionsWindow(EmotionalReactionsService? emotionalReactionsService = null)
        {
            System.Diagnostics.Debug.WriteLine("=== КОНСТРУКТОР ОКНА ЭМОЦИЙ ===");
            System.Diagnostics.Debug.WriteLine($"Переданный сервис: {emotionalReactionsService != null}");
            
            InitializeComponent();
            
            try
            {
                // Проверяем, передан ли сервис
                if (emotionalReactionsService == null)
                {
                    System.Diagnostics.Debug.WriteLine("СЕРВИС НЕ ПЕРЕДАН В КОНСТРУКТОР! СОЗДАЕМ НОВЫЙ...");
                    
                    // Попробуем создать новый сервис
                    try
                    {
                        _emotionalReactionsService = new EmotionalReactionsService();
                        System.Diagnostics.Debug.WriteLine("Новый сервис создан успешно");
                    }
                    catch (Exception createEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"ОШИБКА СОЗДАНИЯ СЕРВИСА: {createEx.Message}");
                        MessageBox.Show($"Ошибка создания сервиса эмоциональных реакций: {createEx.Message}", 
                                       "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Тип переданного сервиса: {emotionalReactionsService.GetType().Name}");
                    _emotionalReactionsService = emotionalReactionsService;
                }
                
                System.Diagnostics.Debug.WriteLine("Загружаем реакции...");
                _emotionalReactions = new ObservableCollection<EmotionalReaction>(_emotionalReactionsService.GetAllReactions());
                System.Diagnostics.Debug.WriteLine($"Загружено реакций: {_emotionalReactions.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ИСКЛЮЧЕНИЕ В КОНСТРУКТОРЕ: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка инициализации окна эмоциональных реакций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            
            ReactionsListBox.ItemsSource = _emotionalReactions;
            
            // Заполняем комбобокс типов эмоций
            EmotionTypeComboBox.ItemsSource = Enum.GetValues(typeof(EmotionType));
            EmotionTypeComboBox.SelectedIndex = 0;
            
            // Привязываем события слайдеров
            PrioritySlider.ValueChanged += (s, e) => PriorityValueText.Text = ((int)e.NewValue).ToString();
            UsageChanceSlider.ValueChanged += (s, e) => UsageChanceValueText.Text = $"{(int)e.NewValue}%";
            CooldownSlider.ValueChanged += (s, e) => CooldownValueText.Text = $"{(int)e.NewValue} сек";
            
            // Устанавливаем начальные значения
            PriorityValueText.Text = "1";
            UsageChanceValueText.Text = "100%";
            CooldownValueText.Text = "0 сек";
            
            // Загружаем глобальные настройки
            GlobalEnabledCheckBox.IsChecked = _emotionalReactionsService.GlobalEnabled;
        }

        private void ReactionsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ReactionsListBox.SelectedItem is EmotionalReaction reaction)
            {
                LoadReactionDetails(reaction);
                // Включаем только поля редактирования, но оставляем глобальные настройки доступными
                SetReactionDetailsEnabled(true);
                _currentReaction = reaction;
                _isEditing = true;
            }
            else
            {
                ClearReactionDetails();
                SetReactionDetailsEnabled(false);
                _currentReaction = null;
                _isEditing = false;
            }
        }

        private void SetReactionDetailsEnabled(bool enabled)
        {
            // Глобальные настройки всегда доступны
            // Остальные поля включаются/выключаются в зависимости от выбора
            ReactionNameTextBox.IsEnabled = enabled;
            ReactionDescriptionTextBox.IsEnabled = enabled;
            EmotionTypeComboBox.IsEnabled = enabled;
            PrioritySlider.IsEnabled = enabled;
            UsageChanceSlider.IsEnabled = enabled;
            CooldownSlider.IsEnabled = enabled;
            IsEnabledCheckBox.IsEnabled = enabled;
            TriggersTextBox.IsEnabled = enabled;
            ResponsesTextBox.IsEnabled = enabled;
            UseUserFilterCheckBox.IsEnabled = enabled;
            AllowAllUsersCheckBox.IsEnabled = enabled;
            AllowedUsersTextBox.IsEnabled = enabled;
            BlockedUsersTextBox.IsEnabled = enabled;
            SaveReactionButton.IsEnabled = enabled;
            CancelReactionButton.IsEnabled = enabled;
        }

        private void LoadReactionDetails(EmotionalReaction reaction)
        {
            ReactionNameTextBox.Text = reaction.Name;
            ReactionDescriptionTextBox.Text = reaction.Description;
            EmotionTypeComboBox.SelectedItem = reaction.Emotion;
            PrioritySlider.Value = reaction.Priority;
            UsageChanceSlider.Value = reaction.UsageChance;
            CooldownSlider.Value = reaction.CooldownSeconds;
            IsEnabledCheckBox.IsChecked = reaction.IsEnabled;
            
            TriggersTextBox.Text = string.Join(Environment.NewLine, reaction.Triggers);
            ResponsesTextBox.Text = string.Join(Environment.NewLine, reaction.Responses);
            
            // Загружаем настройки фильтрации пользователей
            UseUserFilterCheckBox.IsChecked = reaction.UseUserFilter;
            AllowAllUsersCheckBox.IsChecked = reaction.AllowAllUsers;
            AllowedUsersTextBox.Text = string.Join(Environment.NewLine, reaction.AllowedUsers);
            BlockedUsersTextBox.Text = string.Join(Environment.NewLine, reaction.BlockedUsers);
        }

        private void ClearReactionDetails()
        {
            ReactionNameTextBox.Text = string.Empty;
            ReactionDescriptionTextBox.Text = string.Empty;
            EmotionTypeComboBox.SelectedIndex = 0;
            PrioritySlider.Value = 1;
            UsageChanceSlider.Value = 100;
            CooldownSlider.Value = 0;
            IsEnabledCheckBox.IsChecked = true;
            
            TriggersTextBox.Text = string.Empty;
            ResponsesTextBox.Text = string.Empty;
            
            // Очищаем настройки фильтрации пользователей
            UseUserFilterCheckBox.IsChecked = false;
            AllowAllUsersCheckBox.IsChecked = true;
            AllowedUsersTextBox.Text = string.Empty;
            BlockedUsersTextBox.Text = string.Empty;
        }

        private EmotionalReaction CreateReactionFromForm()
        {
            var reaction = new EmotionalReaction
            {
                Name = ReactionNameTextBox.Text.Trim(),
                Description = ReactionDescriptionTextBox.Text.Trim(),
                Emotion = (EmotionType)EmotionTypeComboBox.SelectedItem,
                Priority = (int)PrioritySlider.Value,
                UsageChance = UsageChanceSlider.Value,
                CooldownSeconds = (int)CooldownSlider.Value,
                IsEnabled = IsEnabledCheckBox.IsChecked ?? true,
                Triggers = TriggersTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                Responses = ResponsesTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                
                // Настройки фильтрации пользователей
                UseUserFilter = UseUserFilterCheckBox.IsChecked ?? false,
                AllowAllUsers = AllowAllUsersCheckBox.IsChecked ?? true,
                AllowedUsers = AllowedUsersTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                BlockedUsers = BlockedUsersTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };

            return reaction;
        }

        private void AddReactionButton_Click(object sender, RoutedEventArgs e)
        {
            ClearReactionDetails();
            SetReactionDetailsEnabled(true);
            _currentReaction = new EmotionalReaction();
            _isEditing = false;
            
            // Снимаем выделение с списка
            ReactionsListBox.SelectedItem = null;
        }

        private void EditReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReactionsListBox.SelectedItem is EmotionalReaction reaction)
            {
                LoadReactionDetails(reaction);
                SetReactionDetailsEnabled(true);
                _currentReaction = reaction;
                _isEditing = true;
            }
            else
            {
                MessageBox.Show("Выберите реакцию для редактирования.", "Предупреждение", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReactionsListBox.SelectedItem is EmotionalReaction reaction)
            {
                var result = MessageBox.Show($"Удалить реакцию '{reaction.Name}'?", "Подтверждение", 
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _emotionalReactionsService.RemoveReaction(reaction.Id);
                    _emotionalReactions.Remove(reaction);
                    
                    if (_currentReaction?.Id == reaction.Id)
                    {
                        ClearReactionDetails();
                        ReactionDetailsPanel.IsEnabled = false;
                        _currentReaction = null;
                        _isEditing = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите реакцию для удаления.", "Предупреждение", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReactionNameTextBox.Text))
            {
                MessageBox.Show("Введите название реакции.", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(TriggersTextBox.Text))
            {
                MessageBox.Show("Введите хотя бы один триггер.", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(ResponsesTextBox.Text))
            {
                MessageBox.Show("Введите хотя бы один ответ.", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var reaction = _currentReaction ?? new EmotionalReaction();
                
                reaction.Name = ReactionNameTextBox.Text.Trim();
                reaction.Description = ReactionDescriptionTextBox.Text.Trim();
                reaction.Emotion = (EmotionType)EmotionTypeComboBox.SelectedItem;
                reaction.Priority = (int)PrioritySlider.Value;
                reaction.UsageChance = UsageChanceSlider.Value;
                reaction.CooldownSeconds = (int)CooldownSlider.Value;
                reaction.IsEnabled = IsEnabledCheckBox.IsChecked ?? true;
                
                // Парсим триггеры и ответы
                reaction.Triggers = TriggersTextBox.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                
                reaction.Responses = ResponsesTextBox.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();
                
                // Настройки фильтрации пользователей
                reaction.UseUserFilter = UseUserFilterCheckBox.IsChecked ?? false;
                reaction.AllowAllUsers = AllowAllUsersCheckBox.IsChecked ?? true;
                reaction.AllowedUsers = AllowedUsersTextBox.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrEmpty(u))
                    .ToList();
                reaction.BlockedUsers = BlockedUsersTextBox.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrEmpty(u))
                    .ToList();

                if (!_isEditing)
                {
                    // Добавляем новую реакцию
                    _emotionalReactionsService.AddReaction(reaction);
                    _emotionalReactions.Add(reaction);
                }
                else
                {
                    // Обновляем существующую
                    _emotionalReactionsService.UpdateReaction(reaction);
                    var index = _emotionalReactions.IndexOf(_currentReaction!);
                    if (index >= 0)
                    {
                        _emotionalReactions[index] = reaction;
                    }
                }

                MessageBox.Show("Реакция сохранена успешно!", "Успех", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                
                ClearReactionDetails();
                ReactionDetailsPanel.IsEnabled = false;
                _currentReaction = null;
                _isEditing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing && _currentReaction != null)
            {
                LoadReactionDetails(_currentReaction);
            }
            else
            {
                ClearReactionDetails();
                ReactionDetailsPanel.IsEnabled = false;
                _currentReaction = null;
                _isEditing = false;
            }
        }

        private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сбросить все эмоциональные реакции к настройкам по умолчанию? " +
                                        "Это удалит все ваши настройки.", "Подтверждение", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем файл настроек, чтобы загрузились настройки по умолчанию
                    var filePath = "EmotionalReactions.json";
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    
                    // Пересоздаем сервис
                    _emotionalReactionsService.GetType().GetMethod("LoadEmotionalReactions", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(_emotionalReactionsService, null);
                    
                    // Обновляем список
                    _emotionalReactions.Clear();
                    foreach (var reaction in _emotionalReactionsService.GetAllReactions())
                    {
                        _emotionalReactions.Add(reaction);
                    }
                    
                    ClearReactionDetails();
                    ReactionDetailsPanel.IsEnabled = false;
                    _currentReaction = null;
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

        private void GlobalEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_emotionalReactionsService == null)
                {
                    MessageBox.Show("Ошибка: сервис эмоциональных реакций не инициализирован.", 
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _emotionalReactionsService.GlobalEnabled = true;
                MessageBox.Show("Эмоциональные реакции включены!", "Настройки", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при включении эмоциональных реакций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GlobalEnabledCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_emotionalReactionsService == null)
                {
                    MessageBox.Show("Ошибка: сервис эмоциональных реакций не инициализирован.", 
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _emotionalReactionsService.GlobalEnabled = false;
                MessageBox.Show("Эмоциональные реакции отключены!", "Настройки", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отключении эмоциональных реакций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 