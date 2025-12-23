using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SilentCaster.Models
{
    public class VoiceSettings
    {
        public string SelectedVoice { get; set; } = string.Empty;
        public double Rate { get; set; } = 0.0; // -10 到 10
        public double Volume { get; set; } = 100.0; // 0 到 100
        
        // Настройки для множественных голосов
        public ObservableCollection<VoiceProfile> VoiceProfiles { get; set; } = new ObservableCollection<VoiceProfile>();
        public bool UseMultipleVoices { get; set; } = false;
        public int CurrentVoiceIndex { get; set; } = 0;
    }
} 