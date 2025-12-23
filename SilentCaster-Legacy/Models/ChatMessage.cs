using System;

namespace SilentCaster.Models
{
    public class ChatMessage
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSubscriber { get; set; } = false;
        public bool IsVip { get; set; } = false;
        public bool IsModerator { get; set; } = false;
        public string DisplayText => $"[{Timestamp:HH:mm:ss}] {GetUserBadge()}{Username}: {Message}";
        
        private string GetUserBadge()
        {
            if (IsModerator) return "🛡️ ";
            if (IsVip) return "💎 ";
            if (IsSubscriber) return "⭐ ";
            return "";
        }
    }
} 