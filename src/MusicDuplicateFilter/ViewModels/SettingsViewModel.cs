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

        // 加载设置到 ViewModel
        _includeSubdirectories = _settings.IncludeSubdirectories;
        _fileNameThreshold = _settings.FileNameSimilarityThreshold;
        _metadataThreshold = _settings.MetadataSimilarityThreshold;
        _compareFileSize = _settings.CompareFileSize;
        _fileSizeTolerance = _settings.FileSizeTolerance / 1024; // 转换为 KB
        _enableLogging = _settings.EnableLogging;
        _selectedLanguage = _settings.Language;
        _extensionsText = string.Join("; ", _settings.MusicExtensions);

        Languages = new ObservableCollection<string>(_loc.SupportedLanguages);
    }

    public ObservableCollection<string> Languages { get; }

    // ===== 属性 =====

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

    private int _fileNameThreshold = 80;
    public int FileNameThreshold
    {
        get => _fileNameThreshold;
        set => SetProperty(ref _fileNameThreshold, value);
    }

    private int _metadataThreshold = 85;
    public int MetadataThreshold
    {
        get => _metadataThreshold;
        set => SetProperty(ref _metadataThreshold, value);
    }

    private bool _compareFileSize = true;
    public bool CompareFileSize
    {
        get => _compareFileSize;
        set => SetProperty(ref _compareFileSize, value);
    }

    private long _fileSizeTolerance = 100; // KB
    public long FileSizeTolerance
    {
        get => _fileSizeTolerance;
        set => SetProperty(ref _fileSizeTolerance, value);
    }

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
        // 解析扩展名
        var extensions = ExtensionsText
            .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().StartsWith('.') ? e.Trim() : $".{e.Trim()}")
            .Distinct()
            .ToList();

        if (extensions.Count == 0)
            extensions = [".mp3", ".flac", ".wav", ".ogg", ".wma", ".aac", ".m4a"];

        // 保存到设置对象
        _settings.IncludeSubdirectories = IncludeSubdirectories;
        _settings.MusicExtensions = extensions;
        _settings.FileNameSimilarityThreshold = FileNameThreshold;
        _settings.MetadataSimilarityThreshold = MetadataThreshold;
        _settings.CompareFileSize = CompareFileSize;
        _settings.FileSizeTolerance = FileSizeTolerance * 1024;
        _settings.EnableLogging = EnableLogging;
        _settings.Language = SelectedLanguage;
        _settings.Save();

        // 切换语言
        _loc.SetLanguage(SelectedLanguage);

        System.Windows.MessageBox.Show(
            _loc.GetString("Settings.Saved"),
            _loc.GetString("Settings.Title"),
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        // 关闭窗口
        CloseWindowAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindowAction?.Invoke();
    }

    /// <summary>关闭窗口的动作（由 View 设置）</summary>
    public Action? CloseWindowAction { get; set; }
}
