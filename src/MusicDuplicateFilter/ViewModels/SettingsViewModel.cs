using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MusicDuplicateFilter.ViewModels;

/// <summary>
/// 设置窗口 ViewModel
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly Services.ILocalizationService _loc;
    private readonly Models.AppSettings _settings;

    public SettingsViewModel(Services.ILocalizationService localizationService)
    {
        _loc = localizationService;
        _settings = Models.AppSettings.Load();

        // 加载基本设置
        _includeSubdirectories = _settings.IncludeSubdirectories;
        _compareFileSize = _settings.CompareFileSize;
        _fileSizeTolerance = _settings.FileSizeTolerance / 1024;
        _enableLogging = _settings.EnableLogging;
        _selectedLanguage = _settings.Language;
        _extensionsText = string.Join("; ", _settings.MusicExtensions);
        _maxParallelism = _settings.MaxParallelism;
        _selectedKeepPreference = (int)_settings.KeepPreference;

        // 加载 MatchRule
        var rule = _settings.MatchRule;
        _matchThreshold = rule.Threshold;
        _fileNameWeight = rule.FileNameWeight;
        _titleWeight = rule.TitleWeight;
        _artistWeight = rule.ArtistWeight;
        _albumWeight = rule.AlbumWeight;
        _durationWeight = rule.DurationWeight;
        _durationToleranceSec = rule.DurationToleranceSeconds;

        Languages = new ObservableCollection<string>(_loc.SupportedLanguages);
        KeepPreferenceItems = [
            _loc.GetString("Settings.KeepLargest"),
            _loc.GetString("Settings.KeepSmallest"),
            _loc.GetString("Settings.KeepMostMetadata"),
            _loc.GetString("Settings.KeepNewest"),
            _loc.GetString("Settings.KeepOldest")
        ];
    }

    public Services.LocalizationProvider L => Services.LocalizationProvider.Current;
    public ObservableCollection<string> Languages { get; }
    public ObservableCollection<string> KeepPreferenceItems { get; }

    // ===== 基本设置 =====

    private bool _includeSubdirectories;
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetProperty(ref _includeSubdirectories, value);
    }

    private string _extensionsText = string.Empty;
    public string ExtensionsText
    {
        get => _extensionsText;
        set => SetProperty(ref _extensionsText, value);
    }

    private int _maxParallelism = 4;
    public int MaxParallelism
    {
        get => _maxParallelism;
        set => SetProperty(ref _maxParallelism, Math.Clamp(value, 1, 16));
    }

    private bool _compareFileSize = false;
    public bool CompareFileSize
    {
        get => _compareFileSize;
        set => SetProperty(ref _compareFileSize, value);
    }

    private long _fileSizeTolerance = 100;
    public long FileSizeTolerance
    {
        get => _fileSizeTolerance;
        set => SetProperty(ref _fileSizeTolerance, value);
    }

    private int _selectedKeepPreference;
    public int SelectedKeepPreference
    {
        get => _selectedKeepPreference;
        set => SetProperty(ref _selectedKeepPreference, value);
    }

    // ===== MatchRule 属性 =====

    private double _matchThreshold = 80.0;
    public double MatchThreshold
    {
        get => _matchThreshold;
        set => SetProperty(ref _matchThreshold, value);
    }

    private double _fileNameWeight = 1.0;
    public double FileNameWeight
    {
        get => _fileNameWeight;
        set => SetProperty(ref _fileNameWeight, value);
    }

    private double _titleWeight = 1.5;
    public double TitleWeight
    {
        get => _titleWeight;
        set => SetProperty(ref _titleWeight, value);
    }

    private double _artistWeight = 1.0;
    public double ArtistWeight
    {
        get => _artistWeight;
        set => SetProperty(ref _artistWeight, value);
    }

    private double _albumWeight = 0.5;
    public double AlbumWeight
    {
        get => _albumWeight;
        set => SetProperty(ref _albumWeight, value);
    }

    private double _durationWeight = 1.0;
    public double DurationWeight
    {
        get => _durationWeight;
        set => SetProperty(ref _durationWeight, value);
    }

    private double _durationToleranceSec = 2.0;
    public double DurationToleranceSec
    {
        get => _durationToleranceSec;
        set => SetProperty(ref _durationToleranceSec, value);
    }

    // ===== 其他设置 =====

    private bool _enableLogging = true;
    public bool EnableLogging
    {
        get => _enableLogging;
        set => SetProperty(ref _enableLogging, value);
    }

    private string _selectedLanguage = "zh-CN";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    // ===== 命令 =====

    [RelayCommand]
    private void Save()
    {
        var extensions = ExtensionsText
            .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().StartsWith('.') ? e.Trim() : $".{e.Trim()}")
            .Distinct()
            .ToList();

        if (extensions.Count == 0)
            extensions = [".mp3", ".flac", ".wav", ".ogg", ".wma", ".aac", ".m4a"];

        _settings.IncludeSubdirectories = IncludeSubdirectories;
        _settings.MusicExtensions = extensions;
        _settings.CompareFileSize = CompareFileSize;
        _settings.FileSizeTolerance = FileSizeTolerance * 1024;
        _settings.MaxParallelism = MaxParallelism;
        _settings.KeepPreference = (Models.KeepPreference)SelectedKeepPreference;
        _settings.EnableLogging = EnableLogging;
        _settings.Language = SelectedLanguage;

        _settings.MatchRule = new Models.MatchRule
        {
            Threshold = MatchThreshold,
            FileNameWeight = FileNameWeight,
            TitleWeight = TitleWeight,
            ArtistWeight = ArtistWeight,
            AlbumWeight = AlbumWeight,
            DurationWeight = DurationWeight,
            DurationToleranceSeconds = DurationToleranceSec
        };

        _settings.Save();
        _loc.SetLanguage(SelectedLanguage);

        System.Windows.MessageBox.Show(
            _loc.GetString("Settings.Saved"),
            _loc.GetString("Settings.Title"),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        CloseWindowAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseWindowAction?.Invoke();

    /// <summary>关闭窗口的动作（由 View 设置）</summary>
    public Action? CloseWindowAction { get; set; }
}
