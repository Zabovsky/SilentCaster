using System.Collections.Generic;

namespace SilentCaster.Models
{
    public class ForbiddenWords
    {
        public List<string> Words { get; set; } = new List<string>();
        public bool IsEnabled { get; set; } = true;
        public bool CaseSensitive { get; set; } = false;
    }
} 