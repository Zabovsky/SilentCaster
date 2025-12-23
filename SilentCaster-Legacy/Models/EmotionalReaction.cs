using System;
using System.Collections.Generic;

namespace SilentCaster.Models
{
    public class EmotionalReaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Triggers { get; set; } = new List<string>();
        public List<string> Responses { get; set; } = new List<string>();
        public EmotionType Emotion { get; set; } = EmotionType.Neutral;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public double UsageChance { get; set; } = 100.0;
        public int CooldownSeconds { get; set; } = 0;
        public DateTime? LastUsed { get; set; }
        
        // Новые поля для фильтрации по пользователям
        public List<string> AllowedUsers { get; set; } = new List<string>();
        public List<string> BlockedUsers { get; set; } = new List<string>();
        public bool UseUserFilter { get; set; } = false;
        public bool AllowAllUsers { get; set; } = true; // true = разрешить всех, false = только разрешенные
    }

    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Excited,
        Confused,
        Love,
        Laugh,
        Cry,
        Shocked,
        Proud
    }
} 