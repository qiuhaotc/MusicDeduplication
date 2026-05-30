using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MusicDuplicateFilter.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly Services.IFileScanService _fileScanService;
    private readonly Services.IDuplicateDetector _duplicateDetector;
    private readonly Services.IFileOperationService _fileOperationService;
    private readonly Services.ILogService _logService;
    private readonly Services.ILocalizationService _loc;

    private CancellationTokenSource? _scanCts;

    public MainViewModel(
        Services.IFileScanService fileScanService,
        Services.IDuplicateDetector duplicateDetector,
        Services.IFileOperationService fileOperationService,
        Services.ILogService logService,
        Services.ILocalizationService localizationService)
    {
        _fileScanService = fileScanService;
        _duplicateDetector = duplicateDetector;
        _fileOperationService = fileOperationService;
        _logService = logService;
        _loc = localizationService;
    }

    // ===== 属性 =====

    private string _scanDirectory = string.Empty;
    public string ScanDirectory
    {
        get => _scanDirectory;
        set => SetProperty(ref _scanDirectory, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _scanProgress;
    public int ScanProgress
    {
        get => _scanProgress;
        set => SetProperty(ref _scanProgress, value);
    }

    private int _detectProgress;
    public int DetectProgress
    {
        get => _detectProgress;
        set => SetProperty(ref _detectProgress, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsNotScanning));
                OnPropertyChanged(nameof(HasResults));
            }
        }
    }

    public bool IsNotScanning => !IsScanning;
    public bool HasResults => !IsScanning && DuplicateGroups.Count > 0;

    private string _spaceSavingText = string.Empty;
    public string SpaceSavingText
    {
        get => _spaceSavingText;
        set => SetProperty(ref _spaceSavingText, value);
    }

    private string _filesFoundText = string.Empty;
    public string FilesFoundText
    {
        get => _filesFoundText;
        set => SetProperty(ref _filesFoundText, value);
    }

    public ObservableCollection<Models.DuplicateGroup> DuplicateGroups { get; } = [];

    private Models.DuplicateGroup? _selectedGroup;
    public Models.DuplicateGroup? SelectedGroup
    {
        get => _selectedGroup;
        set => SetProperty(ref _selectedGroup, value);
    }

    // ===== 命令 =====

    /// <summary>浏览目录</summary>
    [RelayCommand]
    private void BrowseDirectory()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = _loc.GetString("Main.ScanDirectory"),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ScanDirectory = dialog.SelectedPath;
        }
    }

    /// <summary>开始扫描</summary>
    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanDirectory))
        {
            BrowseDirectory();
            if (string.IsNullOrWhiteSpace(ScanDirectory))
                return;
        }

        if (!Directory.Exists(ScanDirectory))
        {
            System.Windows.MessageBox.Show(
                $"目录不存在: {ScanDirectory}",
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsScanning = true;
        DuplicateGroups.Clear();
        StatusText = _loc.GetString("Main.Scanning");
        ScanProgress = 0;
        DetectProgress = 0;

        _scanCts = new CancellationTokenSource();
        var settings = Models.AppSettings.Load();

        try
        {
            _logService.Info($"开始扫描目录: {ScanDirectory}");

            // 阶段1: 扫描文件
            var files = await _fileScanService.ScanDirectoryAsync(
                ScanDirectory,
                settings.MusicExtensions,
                settings.IncludeSubdirectories,
                _scanCts.Token);

            FilesFoundText = _loc.GetString("Main.FilesFound", files.Count);
            _logService.Info($"扫描完成，找到 {files.Count} 个音乐文件");

            if (files.Count == 0)
            {
                StatusText = _loc.GetString("Main.NoResults");
                return;
            }

            // 阶段2: 检测重复
            StatusText = "正在检测重复...";

            var progress = new Progress<int>(p => DetectProgress = p);
            var groups = await _duplicateDetector.DetectDuplicatesAsync(
                files, settings, progress, _scanCts.Token);

            foreach (var group in groups)
                DuplicateGroups.Add(group);

            UpdateSummary();
            _logService.Info($"重复检测完成，发现 {groups.Count} 组重复文件");
        }
        catch (OperationCanceledException)
        {
            StatusText = "扫描已取消";
            _logService.Info("扫描被用户取消");
        }
        catch (Exception ex)
        {
            StatusText = $"扫描出错: {ex.Message}";
            _logService.Error("扫描过程出错", ex);
            System.Windows.MessageBox.Show(
                $"扫描过程中发生错误:\n{ex.Message}",
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    /// <summary>停止扫描</summary>
    [RelayCommand]
    private void StopScan()
    {
        _scanCts?.Cancel();
    }

    /// <summary>删除选中文件</summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selectedFiles = DuplicateGroups
            .SelectMany(g => g.Files)
            .Where(f => f.IsSelectedForDeletion)
            .Select(f => f.FileInfo.FilePath)
            .ToList();

        if (selectedFiles.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "请先选择要删除的文件",
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        // 确认对话框
        var message = _loc.GetString("Delete.ConfirmMessage", selectedFiles.Count);
        var result = System.Windows.MessageBox.Show(
            message,
            _loc.GetString("Delete.ConfirmTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            StatusText = "正在删除...";

            var progress = new Progress<int>(p => ScanProgress = p);
            var successCount = await _fileOperationService.MoveToRecycleBinAsync(selectedFiles, progress);

            _logService.LogDeletion(selectedFiles.Take(successCount));

            // 从列表中移除已删除的文件
            foreach (var group in DuplicateGroups.ToList())
            {
                var toRemove = group.Files
                    .Where(f => f.IsSelectedForDeletion && f.FileInfo.FilePath != null)
                    .ToList();
                foreach (var item in toRemove)
                    group.Files.Remove(item);

                // 如果组内只剩一个文件，移除整个组
                if (group.Files.Count <= 1)
                    DuplicateGroups.Remove(group);
            }

            UpdateSummary();

            System.Windows.MessageBox.Show(
                _loc.GetString("Delete.Success", successCount),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.Error("删除文件时出错", ex);
            System.Windows.MessageBox.Show(
                _loc.GetString("Delete.Failed"),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            StatusText = _loc.GetString("Main.Ready");
        }
    }

    /// <summary>打开设置窗口</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        var settingsVm = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new Views.SettingsWindow(settingsVm);
        settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }

    /// <summary>预览选中的重复组</summary>
    [RelayCommand]
    private void PreviewGroup()
    {
        if (SelectedGroup == null) return;

        var previewVm = new DuplicatePreviewViewModel(SelectedGroup, _fileOperationService, _loc);
        var previewWindow = new Views.DuplicatePreviewWindow(previewVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        previewWindow.ShowDialog();
    }

    /// <summary>全选</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var group in DuplicateGroups)
        foreach (var file in group.Files.Where(f => !f.IsKeepSuggested))
            file.IsSelectedForDeletion = true;

        foreach (var group in DuplicateGroups)
        foreach (var file in group.Files.Where(f => f.IsKeepSuggested))
            file.IsSelectedForDeletion = false;

        UpdateSummary();
    }

    /// <summary>取消全选</summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var group in DuplicateGroups)
        foreach (var file in group.Files)
            file.IsSelectedForDeletion = false;

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var totalSelectedSize = DuplicateGroups.Sum(g =>
            g.Files.Where(f => f.IsSelectedForDeletion).Sum(f => f.FileInfo.FileSize));

        SpaceSavingText = totalSelectedSize switch
        {
            >= 1_000_000_000 => $"{totalSelectedSize / 1_000_000_000.0:F2} GB",
            >= 1_000_000 => $"{totalSelectedSize / 1_000_000.0:F2} MB",
            >= 1_000 => $"{totalSelectedSize / 1000.0:F2} KB",
            _ => $"{totalSelectedSize} B"
        };

        OnPropertyChanged(nameof(HasResults));
    }
}
