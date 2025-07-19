using System.Collections.Generic;

namespace SilentCaster.Models
{
    public class VoiceSettings
    {
        public string SelectedVoice { get; set; } = string.Empty;
        public double Rate { get; set; } = 0.0; // -10 到 10
        public double Volume { get; set; } = 100.0; // 0 到 100
        
        // Настройки для множественных голосов
        public List<VoiceProfile> VoiceProfiles { get; set; } = new List<VoiceProfile>();
        public bool UseMultipleVoices { get; set; } = false;
        public int CurrentVoiceIndex { get; set; } = 0;
    }

    public class VoiceProfile
    {
        public string Name { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
        public double Rate { get; set; } = 0.0;
        public double Volume { get; set; } = 100.0;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        
        // Настройки взаимодействия
        public bool UseForChatMessages { get; set; } = true;
        public bool UseForQuickResponses { get; set; } = true;
        public bool UseForManualMessages { get; set; } = true;
        public int Priority { get; set; } = 1; // Приоритет использования (1-10)
        public double UsageChance { get; set; } = 100.0; // Шанс использования в процентах
    }
} 