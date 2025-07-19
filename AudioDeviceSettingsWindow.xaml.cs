using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SilentCaster.Services;
using NAudio.Wave;

namespace SilentCaster
{
    public partial class AudioDeviceSettingsWindow : Window
    {
        private readonly AudioDeviceService _audioDeviceService;
        private readonly ObservableCollection<AudioDeviceViewModel> _devices;
        private AudioDeviceViewModel? _selectedDevice;
        private AudioDeviceService.AudioDeviceConfig _originalConfig;

        public AudioDeviceSettingsWindow(AudioDeviceService audioDeviceService)
        {
            try
            {
                InitializeComponent();
                _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
                _devices = new ObservableCollection<AudioDeviceViewModel>();
                _originalConfig = _audioDeviceService.GetConfig();
                
                if (AudioDevicesListBox != null)
                {
                    AudioDevicesListBox.ItemsSource = _devices;
                }
                
                LoadDevices();
                UpdateDeviceInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AudioDeviceSettingsWindow constructor: {ex.Message}");
                MessageBox.Show($"Ошибка инициализации окна настроек аудио устройств: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void LoadDevices()
        {
            try
            {
                if (_audioDeviceService == null)
                {
                    System.Diagnostics.Debug.WriteLine("AudioDeviceService is null in LoadDevices");
                    return;
                }
                
                if (_devices == null)
                {
                    System.Diagnostics.Debug.WriteLine("_devices collection is null in LoadDevices");
                    return;
                }
                
                _devices.Clear();
                var devices = _audioDeviceService.GetAvailableDevices();
                var selectedDevice = _audioDeviceService.GetSelectedDevice();

                foreach (var device in devices)
                {
                    var isSelected = selectedDevice?.DeviceId == device.DeviceId;
                    var isAvailable = _audioDeviceService.TestDevice(device.DeviceId);
                    
                    _devices.Add(new AudioDeviceViewModel
                    {
                        DeviceId = device.DeviceId,
                        Name = device.Name,
                        DeviceInfo = $"Каналы: {device.Channels}, Частота: {device.SampleRate} Hz",
                        IsSelected = isSelected,
                        Status = isAvailable ? "Доступно" : "Недоступно",
                        StatusColor = isAvailable ? Brushes.LightGreen : Brushes.Red,
                        Channels = device.Channels,
                        SampleRate = device.SampleRate
                    });
                }

                // Выбираем текущее устройство
                var currentDevice = _devices.FirstOrDefault(d => d.IsSelected);
                if (currentDevice != null && AudioDevicesListBox != null)
                {
                    try
                    {
                        AudioDevicesListBox.SelectedItem = currentDevice;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error setting selected item: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadDevices: {ex.Message}");
            }
        }

        private void UpdateDeviceInfo()
        {
            try
            {
                // Проверяем, что все элементы интерфейса инициализированы
                if (DeviceNameTextBlock == null || DeviceIdTextBlock == null || 
                    DeviceChannelsTextBlock == null || DeviceSampleRateTextBlock == null || 
                    UseCustomDeviceCheckBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("Some UI elements are not initialized in UpdateDeviceInfo");
                    return;
                }
                
                if (_selectedDevice != null)
                {
                    DeviceNameTextBlock.Text = _selectedDevice.Name;
                    DeviceIdTextBlock.Text = _selectedDevice.DeviceId;
                    DeviceChannelsTextBlock.Text = _selectedDevice.Channels.ToString();
                    DeviceSampleRateTextBlock.Text = $"{_selectedDevice.SampleRate} Hz";
                    UseCustomDeviceCheckBox.IsChecked = _selectedDevice.DeviceId != "default";
                }
                else
                {
                    DeviceNameTextBlock.Text = "Не выбрано";
                    DeviceIdTextBlock.Text = "-";
                    DeviceChannelsTextBlock.Text = "-";
                    DeviceSampleRateTextBlock.Text = "-";
                    UseCustomDeviceCheckBox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateDeviceInfo: {ex.Message}");
            }
        }

        private void AudioDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AudioDevicesListBox?.SelectedItem is AudioDeviceViewModel selectedDevice)
                {
                    if (_devices == null)
                    {
                        System.Diagnostics.Debug.WriteLine("_devices collection is null in AudioDevicesListBox_SelectionChanged");
                        return;
                    }
                    
                    // Снимаем выделение с предыдущего устройства
                    foreach (var device in _devices)
                    {
                        device.IsSelected = false;
                    }
                    
                    // Выбираем новое устройство
                    selectedDevice.IsSelected = true;
                    _selectedDevice = selectedDevice;
                    
                    UpdateDeviceInfo();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AudioDevicesListBox_SelectionChanged: {ex.Message}");
            }
        }

        private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDevices();
            UpdateDeviceInfo();
        }

        private void TestDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null)
            {
                MessageBox.Show("Выберите устройство для тестирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var isAvailable = _audioDeviceService.TestDevice(_selectedDevice.DeviceId);
            var message = isAvailable ? "Устройство доступно и работает корректно!" : "Устройство недоступно или не работает.";
            var icon = isAvailable ? MessageBoxImage.Information : MessageBoxImage.Warning;
            
            MessageBox.Show(message, "Результат тестирования", MessageBoxButton.OK, icon);
        }

        private async void TestSoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null)
            {
                MessageBox.Show("Выберите устройство для тестирования звука", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Отключаем кнопку на время воспроизведения
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                    button.Content = "🎵 Воспроизведение...";
                }
                
                await PlayTestSoundAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения тестового звука: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем кнопку
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                    button.Content = "🎵 Тест звука";
                }
            }
        }

        private async Task PlayTestSoundAsync()
        {
            // Используем фиксированную громкость для избежания проблем с UI элементами
            double volume = 0.5;

            await Task.Run(async () =>
            {
                try
                {
                    // Создаем простой тестовый звук (синусоида 440 Hz)
                    var sampleRate = 44100;
                    var duration = 1.0; // 1 секунда
                    var frequency = 440.0; // 440 Hz (нота A)
                    var amplitude = 0.3;

                    var samples = (int)(sampleRate * duration);
                    var waveBuffer = new byte[samples * 2]; // 16-bit audio

                    for (int i = 0; i < samples; i++)
                    {
                        var sample = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * amplitude * volume * short.MaxValue);
                        var bytes = BitConverter.GetBytes(sample);
                        waveBuffer[i * 2] = bytes[0];
                        waveBuffer[i * 2 + 1] = bytes[1];
                    }

                    // Воспроизводим звук
                    using var waveStream = new RawSourceWaveStream(waveBuffer, 0, waveBuffer.Length, new WaveFormat(sampleRate, 16, 1));
                    
                    if (_selectedDevice?.DeviceId == "default")
                    {
                        using var waveOut = new WaveOut();
                        waveOut.Init(waveStream);
                        waveOut.Play();
                        
                        // Ждем окончания воспроизведения асинхронно
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(50); // Используем await вместо Wait()
                        }
                    }
                    else
                    {
                        // Для конкретного устройства
                        var deviceId = int.Parse(_selectedDevice!.DeviceId);
                        using var waveOut = new WaveOut { DeviceNumber = deviceId };
                        waveOut.Init(waveStream);
                        waveOut.Play();
                        
                        // Ждем окончания воспроизведения асинхронно
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(50); // Используем await вместо Wait()
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in PlayTestSoundAsync: {ex.Message}");
                    throw;
                }
            });
        }

        private void TestVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (TestVolumeTextBlock != null)
                {
                    TestVolumeTextBlock.Text = $"{(int)e.NewValue}%";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TestVolumeSlider_ValueChanged: {ex.Message}");
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice != null)
            {
                _audioDeviceService.SetSelectedDevice(_selectedDevice.DeviceId);
                MessageBox.Show($"Устройство '{_selectedDevice.Name}' применено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice != null)
            {
                _audioDeviceService.SetSelectedDevice(_selectedDevice.DeviceId);
            }
            
            MessageBox.Show("Настройки аудио устройств сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Восстанавливаем оригинальные настройки
            _audioDeviceService.UpdateConfig(_originalConfig);
            DialogResult = false;
            Close();
        }

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
            Close();
        }

        public class AudioDeviceViewModel
        {
            public string DeviceId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string DeviceInfo { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public string Status { get; set; } = string.Empty;
            public Brush StatusColor { get; set; } = Brushes.Gray;
            public int Channels { get; set; }
            public int SampleRate { get; set; }
        }
    }
} 