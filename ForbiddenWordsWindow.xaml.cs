using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class ForbiddenWordsWindow : Window
    {
        private readonly ForbiddenWordsService _forbiddenWordsService;
        private readonly ObservableCollection<string> _forbiddenWords;
        private ForbiddenWords _originalSettings;

        public ForbiddenWordsWindow(ForbiddenWordsService forbiddenWordsService)
        {
            InitializeComponent();
            _forbiddenWordsService = forbiddenWordsService;
            
            // Загружаем текущие настройки
            var settings = _forbiddenWordsService.GetForbiddenWords();
            _originalSettings = new ForbiddenWords
            {
                IsEnabled = settings.IsEnabled,
                CaseSensitive = settings.CaseSensitive,
                Words = new List<string>(settings.Words)
            };
            
            // Инициализируем коллекцию
            _forbiddenWords = new ObservableCollection<string>(settings.Words);
            ForbiddenWordsListBox.ItemsSource = _forbiddenWords;
            
            // Устанавливаем настройки
            IsEnabledCheckBox.IsChecked = settings.IsEnabled;
            CaseSensitiveCheckBox.IsChecked = settings.CaseSensitive;
            
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            TotalWordsTextBlock.Text = _forbiddenWords.Count.ToString();
            FilterStatusTextBlock.Text = IsEnabledCheckBox.IsChecked == true ? "Включен" : "Отключен";
            FilterStatusTextBlock.Foreground = IsEnabledCheckBox.IsChecked == true ? 
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        }

        private void AddWordButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewWord();
        }

        private void NewWordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewWord();
            }
        }

        private void AddNewWord()
        {
            var word = NewWordTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(word))
            {
                if (!_forbiddenWords.Contains(word))
                {
                    _forbiddenWords.Add(word);
                    NewWordTextBox.Text = string.Empty;
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Это слово уже есть в списке!", "Предупреждение", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RemoveWordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string word)
            {
                var result = MessageBox.Show($"Удалить слово '{word}'?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _forbiddenWords.Remove(word);
                    UpdateStatistics();
                }
            }
        }

        private void ResetToDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сбросить список к стандартным запрещенным словам Twitch и YouTube?\n" +
                "Все текущие изменения будут потеряны!", "Подтверждение", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                // Создаем новый сервис для получения стандартных слов
                var tempService = new ForbiddenWordsService();
                var defaultSettings = tempService.GetForbiddenWords();
                
                _forbiddenWords.Clear();
                foreach (var word in defaultSettings.Words)
                {
                    _forbiddenWords.Add(word);
                }
                
                IsEnabledCheckBox.IsChecked = defaultSettings.IsEnabled;
                CaseSensitiveCheckBox.IsChecked = defaultSettings.CaseSensitive;
                
                UpdateStatistics();
                MessageBox.Show("Список сброшен к стандартным настройкам!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = "txt",
                FileName = "forbidden_words.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var content = string.Join(Environment.NewLine, _forbiddenWords);
                    File.WriteAllText(saveFileDialog.FileName, content);
                    MessageBox.Show("Список успешно экспортирован!", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = "txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var content = File.ReadAllText(openFileDialog.FileName);
                    var words = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => w.Trim())
                        .Where(w => !string.IsNullOrEmpty(w))
                        .ToList();

                    var result = MessageBox.Show($"Импортировать {words.Count} слов?\n" +
                        "Существующие слова будут заменены.", "Подтверждение", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _forbiddenWords.Clear();
                        foreach (var word in words)
                        {
                            _forbiddenWords.Add(word);
                        }
                        UpdateStatistics();
                        MessageBox.Show("Список успешно импортирован!", "Успех", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем настройки
                var isEnabled = IsEnabledCheckBox.IsChecked ?? true;
                var caseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
                
                _forbiddenWordsService.UpdateSettings(isEnabled, caseSensitive);
                
                // Обновляем список слов
                var currentWords = _forbiddenWordsService.GetForbiddenWords();
                currentWords.Words.Clear();
                currentWords.Words.AddRange(_forbiddenWords);
                
                // Сохраняем изменения
                _forbiddenWordsService.UpdateAllWords(_forbiddenWords.ToList());
                
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