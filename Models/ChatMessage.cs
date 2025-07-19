using System;

namespace SilentCaster.Models
{
    public class ChatMessage
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Username}: {Message}";
    }
} 