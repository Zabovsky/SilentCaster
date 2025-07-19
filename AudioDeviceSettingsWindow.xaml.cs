using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            InitializeComponent();
            _audioDeviceService = audioDeviceService;
            _devices = new ObservableCollection<AudioDeviceViewModel>();
            _originalConfig = _audioDeviceService.GetConfig();
            
            AudioDevicesListBox.ItemsSource = _devices;
            LoadDevices();
            UpdateDeviceInfo();
        }

        private void LoadDevices()
        {
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
            if (currentDevice != null)
            {
                AudioDevicesListBox.SelectedItem = currentDevice;
            }
        }

        private void UpdateDeviceInfo()
        {
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

        private void AudioDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioDevicesListBox.SelectedItem is AudioDeviceViewModel selectedDevice)
            {
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

        private void TestSoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null)
            {
                MessageBox.Show("Выберите устройство для тестирования звука", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PlayTestSound();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения тестового звука: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayTestSound()
        {
            // Создаем простой тестовый звук (синусоида 440 Hz)
            var sampleRate = 44100;
            var duration = 1.0; // 1 секунда
            var frequency = 440.0; // 440 Hz (нота A)
            var amplitude = 0.3;
            var volume = TestVolumeSlider.Value / 100.0;

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
                
                // Ждем окончания воспроизведения
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            else
            {
                // Для конкретного устройства
                var deviceId = int.Parse(_selectedDevice!.DeviceId);
                using var waveOut = new WaveOut { DeviceNumber = deviceId };
                waveOut.Init(waveStream);
                waveOut.Play();
                
                // Ждем окончания воспроизведения
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        private void TestVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TestVolumeTextBlock.Text = $"{(int)e.NewValue}%";
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