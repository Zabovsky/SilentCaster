using System.ComponentModel;

public class VoiceProfile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private string _name = string.Empty;
    public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } } }

    private string _voiceName = string.Empty;
    public string VoiceName { get => _voiceName; set { if (_voiceName != value) { _voiceName = value; OnPropertyChanged(nameof(VoiceName)); } } }

    private double _rate;
    public double Rate { get => _rate; set { if (_rate != value) { _rate = value; OnPropertyChanged(nameof(Rate)); } } }

    private double _volume;
    public double Volume { get => _volume; set { if (_volume != value) { _volume = value; OnPropertyChanged(nameof(Volume)); } } }

    private bool _isEnabled;
    public bool IsEnabled { get => _isEnabled; set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); } } }

    private string _description = string.Empty;
    public string Description { get => _description; set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } } }

    private bool _useForChatMessages;
    public bool UseForChatMessages { get => _useForChatMessages; set { if (_useForChatMessages != value) { _useForChatMessages = value; OnPropertyChanged(nameof(UseForChatMessages)); } } }

    private bool _useForQuickResponses;
    public bool UseForQuickResponses { get => _useForQuickResponses; set { if (_useForQuickResponses != value) { _useForQuickResponses = value; OnPropertyChanged(nameof(UseForQuickResponses)); } } }

    private bool _useForManualMessages;
    public bool UseForManualMessages { get => _useForManualMessages; set { if (_useForManualMessages != value) { _useForManualMessages = value; OnPropertyChanged(nameof(UseForManualMessages)); } } }

    private int _priority;
    public int Priority { get => _priority; set { if (_priority != value) { _priority = value; OnPropertyChanged(nameof(Priority)); } } }

    private double _usageChance;
    public double UsageChance { get => _usageChance; set { if (_usageChance != value) { _usageChance = value; OnPropertyChanged(nameof(UsageChance)); } } }
} 