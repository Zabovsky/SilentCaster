using System.Collections.Generic;

namespace SilentCaster.Models
{
    public class QuickResponse
    {
        public string Trigger { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public List<string> Responses { get; set; } = new List<string>();
        public string Category { get; set; } = "Общие";
        public int Priority { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
        public bool UseForChatMessages { get; set; } = true;
        public bool UseForManualMessages { get; set; } = true;
        public bool UseForQuickResponses { get; set; } = true;
        public double UsageChance { get; set; } = 100;
        public double Delay { get; set; } = 0;
    }
} 