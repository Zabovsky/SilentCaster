using System;
using System.IO;
using System.Text.Json;
using SilentCaster.Models;

namespace SilentCaster.Services
{
    public class SettingsService
    {
        private const string SettingsFileName = "app_settings.json";
        private readonly string _settingsPath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SilentCaster");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _settingsPath = Path.Combine(appFolder, SettingsFileName);
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
            
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }
    }

    public class AppSettings
    {
        public bool AlwaysOnTop { get; set; } = true;
        public bool UseMultipleVoices { get; set; } = false;
        public string LastUsername { get; set; } = string.Empty;
        public string LastChannel { get; set; } = string.Empty;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public bool WindowMaximized { get; set; } = false;
        public VoiceSettings VoiceSettings { get; set; } = new VoiceSettings();
        
        // Новые настройки для озвучки чата
        public bool EnableChatVoice { get; set; } = true;
        public string ChatTriggerSymbol { get; set; } = "!!!";
        public bool VoiceOnlyForSubscribers { get; set; } = false;
        public bool VoiceOnlyForVips { get; set; } = false;
        public bool VoiceOnlyForModerators { get; set; } = false;
        
        // Настройки чата
        public int MaxChatMessages { get; set; } = 100;
    }
} 