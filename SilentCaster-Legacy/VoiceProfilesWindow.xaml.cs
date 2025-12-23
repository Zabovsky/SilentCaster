using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SilentCaster.Models;
using SilentCaster.Services;

namespace SilentCaster
{
    public partial class VoiceProfilesWindow : Window
    {
        private readonly SpeechService _speechService;
        private readonly ObservableCollection<VoiceProfile> _voiceProfiles;
        private VoiceProfile? _currentProfile;

        public VoiceProfilesWindow(SpeechService speechService, VoiceSettings voiceSettings)
        {
            InitializeComponent();
            
            _speechService = speechService;
            _voiceProfiles = new ObservableCollection<VoiceProfile>();
            
            // Загружаем существующие профили
            foreach (VoiceProfile profile in voiceSettings.VoiceProfiles)
            {
                _voiceProfiles.Add(profile);
            }
            
            VoiceProfilesListBox.ItemsSource = _voiceProfiles;
            
            // Загружаем доступные голоса
            var voices = _speechService.GetAvailableVoices();
            VoicesListBox.ItemsSource = voices;
            
            // Устанавливаем обработчики событий для слайдеров
            RateSlider.ValueChanged += RateSlider_ValueChanged;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            PrioritySlider.ValueChanged += PrioritySlider_ValueChanged;
            UsageChanceSlider.ValueChanged += UsageChanceSlider_ValueChanged;
            
            UpdateStatistics();
        }

        private void VoiceProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VoiceProfilesListBox.SelectedItem is VoiceProfile selectedProfile)
            {
                LoadProfile(selectedProfile);
                ShowProfileSettings();
            }
            else
            {
                HideProfileSettings();
            }
        }

        private void ShowProfileSettings()
        {
            ProfileSettingsBorder.Visibility = Visibility.Visible;
            NoProfileBorder.Visibility = Visibility.Collapsed;
        }

        private void HideProfileSettings()
        {
            ProfileSettingsBorder.Visibility = Visibility.Collapsed;
            NoProfileBorder.Visibility = Visibility.Visible;
        }

        private void UpdateStatistics()
        {
            TotalProfilesTextBlock.Text = _voiceProfiles.Count.ToString();
            var activeCount = _voiceProfiles.Count(p => p.IsEnabled);
            ActiveProfilesTextBlock.Text = activeCount.ToString();
        }

        private void LoadProfile(VoiceProfile profile)
        {
            _currentProfile = profile;
            
            // Отключаем обработчики событий для предотвращения циклических обновлений
            RateSlider.ValueChanged -= RateSlider_ValueChanged;
            VolumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
            PrioritySlider.ValueChanged -= PrioritySlider_ValueChanged;
            UsageChanceSlider.ValueChanged -= UsageChanceSlider_ValueChanged;
            
            // Убеждаемся, что название не пустое
            if (string.IsNullOrEmpty(profile.Name))
            {
                profile.Name = "Новый профиль";
            }
            
            ProfileNameTextBox.Text = profile.Name;
            ProfileNameTextBox.Focus(); // Фокусируемся на поле названия
            ProfileNameTextBox.SelectAll(); // Выделяем весь текст для удобного редактирования
            
            // Выделяем голос в списке
            if (!string.IsNullOrEmpty(profile.VoiceName))
            {
                VoicesListBox.SelectedItem = profile.VoiceName;
            }
            else if (VoicesListBox.Items.Count > 0)
            {
                VoicesListBox.SelectedIndex = 0;
            }
            
            DescriptionTextBox.Text = profile.Description;
            RateSlider.Value = profile.Rate;
            VolumeSlider.Value = profile.Volume;
            IsEnabledCheckBox.IsChecked = profile.IsEnabled;
            
            // Настройки взаимодействия
            UseForChatMessagesCheckBox.IsChecked = profile.UseForChatMessages;
            UseForQuickResponsesCheckBox.IsChecked = profile.UseForQuickResponses;
            UseForManualMessagesCheckBox.IsChecked = profile.UseForManualMessages;
            PrioritySlider.Value = profile.Priority;
            UsageChanceSlider.Value = profile.UsageChance;
            
            RateTextBlock.Text = ((int)profile.Rate).ToString();
            VolumeTextBlock.Text = $"{(int)profile.Volume}%";
            PriorityTextBlock.Text = ((int)profile.Priority).ToString();
            UsageChanceTextBlock.Text = $"{(int)profile.UsageChance}%";
            
            // Восстанавливаем обработчики событий
            RateSlider.ValueChanged += RateSlider_ValueChanged;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            PrioritySlider.ValueChanged += PrioritySlider_ValueChanged;
            UsageChanceSlider.ValueChanged += UsageChanceSlider_ValueChanged;
            
            UpdateStatistics();
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var voices = _speechService.GetAvailableVoices();
            if (!voices.Any())
            {
                MessageBox.Show("Нет доступных голосов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var newProfile = _speechService.CreateVoiceProfile(
                $"Новый профиль {_voiceProfiles.Count + 1}",
                voices.First(),
                0,
                100,
                "Описание нового профиля"
            );
            
            _voiceProfiles.Add(newProfile);
            VoiceProfilesListBox.SelectedItem = newProfile;
            
            UpdateStatistics();
        }

        private void RemoveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (VoiceProfilesListBox.SelectedItem is VoiceProfile selectedProfile)
            {
                var result = MessageBox.Show(
                    $"Удалить профиль '{selectedProfile.Name}'?", 
                    "Подтверждение", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    _voiceProfiles.Remove(selectedProfile);
                    ClearForm();
                    UpdateStatistics();
                }
            }
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == null)
            {
                MessageBox.Show("Выберите профиль для редактирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
            {
                MessageBox.Show("Введите название профиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (VoicesListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите голос", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Обновляем профиль
            _currentProfile.Name = ProfileNameTextBox.Text.Trim();
            _currentProfile.VoiceName = VoicesListBox.SelectedItem.ToString() ?? string.Empty;
            _currentProfile.Description = DescriptionTextBox.Text.Trim();
            _currentProfile.Rate = RateSlider.Value;
            _currentProfile.Volume = VolumeSlider.Value;
            _currentProfile.IsEnabled = IsEnabledCheckBox.IsChecked ?? true;
            
            // Настройки взаимодействия
            _currentProfile.UseForChatMessages = UseForChatMessagesCheckBox.IsChecked ?? true;
            _currentProfile.UseForQuickResponses = UseForQuickResponsesCheckBox.IsChecked ?? true;
            _currentProfile.UseForManualMessages = UseForManualMessagesCheckBox.IsChecked ?? true;
            _currentProfile.Priority = (int)PrioritySlider.Value;
            _currentProfile.UsageChance = UsageChanceSlider.Value;
            
            // Обновляем отображение в списке
            VoiceProfilesListBox.Items.Refresh();
            UpdateStatistics();
            
            MessageBox.Show($"Профиль '{_currentProfile.Name}' сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TestVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == null)
            {
                MessageBox.Show("Выберите профиль для тестирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                _speechService.SpeakTestAsync(_currentProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RateTextBlock != null)
            {
                RateTextBlock.Text = ((int)e.NewValue).ToString();
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeTextBlock != null)
            {
                VolumeTextBlock.Text = $"{(int)e.NewValue}%";
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

        private void ClearForm()
        {
            _currentProfile = null;
            ProfileNameTextBox.Text = string.Empty;
            VoicesListBox.SelectedIndex = -1;
            DescriptionTextBox.Text = string.Empty;
            RateSlider.Value = 0;
            VolumeSlider.Value = 100;
            IsEnabledCheckBox.IsChecked = true;
        }

        public VoiceSettings GetUpdatedVoiceSettings(VoiceSettings originalSettings)
        {
            originalSettings.VoiceProfiles.Clear();
            foreach (VoiceProfile profile in _voiceProfiles)
            {
                originalSettings.VoiceProfiles.Add(profile);
            }
            return originalSettings;
        }

        // Обработчики управления окном
        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем текущий профиль если он выбран
                if (_currentProfile != null)
                {
                    SaveProfileButton_Click(sender, e);
                }
                
                MessageBox.Show("Все профили сохранены!", "Успех", 
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

        private void VoicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentProfile != null && VoicesListBox.SelectedItem != null)
            {
                _currentProfile.VoiceName = VoicesListBox.SelectedItem.ToString() ?? string.Empty;
            }
        }
    }
} 