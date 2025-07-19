using System;
using System.Collections.Generic;

namespace SilentCaster.Models
{
    public class StreamEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EventType Type { get; set; } = EventType.Donation;
        public List<string> Responses { get; set; } = new List<string>();
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public double UsageChance { get; set; } = 100.0;
        
        // Настройки для разных типов событий
        public decimal? MinAmount { get; set; } // для донатов
        public decimal? MaxAmount { get; set; } // для донатов
        public int? SubscriberMonths { get; set; } // для подписок
        public int? RaidViewers { get; set; } // для рейдов
    }

    public enum EventType
    {
        Donation,
        Subscription,
        Resubscription,
        GiftSubscription,
        Raid,
        Follow,
        Bits,
        Host,
        Cheer
    }
} 