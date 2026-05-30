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

        // 加载已保存的目录列表
        var settings = Models.AppSettings.Load();
        foreach (var dir in settings.ScanDirectories)
            ScanDirectories.Add(dir);
    }

    // ===== 本地化代理（XAML 通过 {Binding L[Key]} 绑定）=====
    public Services.LocalizationProvider L => Services.LocalizationProvider.Current;

    // ===== 属性 =====

    /// <summary>扫描目录列表</summary>
    public ObservableCollection<string> ScanDirectories { get; } = [];

    private string _newDirectoryInput = string.Empty;
    /// <summary>新目录输入框内容</summary>
    public string NewDirectoryInput
    {
        get => _newDirectoryInput;
        set => SetProperty(ref _newDirectoryInput, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _overallProgress;
    /// <summary>总体扫描进度（0-100），用于底部进度条</summary>
    public int OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value);
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

    // ===== 命令 =====

    /// <summary>将输入框中的目录添加到列表；若输入为空则打开文件夹选择对话框</summary>
    [RelayCommand]
    private void AddDirectory()
    {
        var dir = NewDirectoryInput.Trim();
        if (string.IsNullOrEmpty(dir))
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = _loc.GetString("Main.ScanDirectory"),
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                AddDirectoryToList(dialog.SelectedPath);
        }
        else
        {
            AddDirectoryToList(dir);
            NewDirectoryInput = string.Empty;
        }
    }

    /// <summary>从列表中移除目录</summary>
    [RelayCommand]
    private void RemoveDirectory(string? dir)
    {
        if (!string.IsNullOrEmpty(dir))
            ScanDirectories.Remove(dir);
    }

    /// <summary>清空目录列表并保存设置</summary>
    [RelayCommand]
    private void ClearDirectories()
    {
        ScanDirectories.Clear();
        var settings = Models.AppSettings.Load();
        settings.ScanDirectories = [];
        settings.Save();
    }

    private void AddDirectoryToList(string dir)
    {
        if (!ScanDirectories.Contains(dir, StringComparer.OrdinalIgnoreCase))
            ScanDirectories.Add(dir);
    }

    /// <summary>开始扫描</summary>
    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (ScanDirectories.Count == 0)
        {
            System.Windows.MessageBox.Show(
                _loc.GetString("Main.NoDirectories"),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            AddDirectory();
            if (ScanDirectories.Count == 0) return;
        }

        IsScanning = true;
        DuplicateGroups.Clear();
        StatusText = _loc.GetString("Main.Scanning");
        OverallProgress = 0;
        FilesFoundText = string.Empty;
        SpaceSavingText = string.Empty;

        _scanCts = new CancellationTokenSource();
        var settings = Models.AppSettings.Load();

        try
        {
            var dirs = ScanDirectories.ToList();
            _logService.Info($"Starting scan of {dirs.Count} directories");

            // 阶段1：扫描文件（进度 0-40%）
            _fileScanService.ProgressChanged += OnScanProgress;
            List<Models.MusicFileInfo> files;
            try
            {
                files = await _fileScanService.ScanDirectoriesAsync(
                    dirs,
                    settings.MusicExtensions,
                    settings.IncludeSubdirectories,
                    settings.MaxParallelism,
                    _scanCts.Token);
            }
            finally
            {
                _fileScanService.ProgressChanged -= OnScanProgress;
            }

            FilesFoundText = _loc.GetString("Main.FilesFound", files.Count);
            _logService.Info($"Scan complete, found {files.Count} music files");

            if (files.Count == 0)
            {
                StatusText = _loc.GetString("Main.NoResults");
                OverallProgress = 100;
                return;
            }

            // 阶段2：检测重复（进度 40-100%）
            StatusText = _loc.GetString("Main.Detecting");

            var progress = new Progress<int>(p => OverallProgress = 40 + p * 60 / 100);
            var groups = await _duplicateDetector.DetectDuplicatesAsync(
                files, settings, progress, _scanCts.Token);

            foreach (var group in groups)
                DuplicateGroups.Add(group);

            // 保存目录列表到设置
            settings.ScanDirectories = ScanDirectories.ToList();
            settings.Save();

            UpdateSummary();
            StatusText = _loc.GetString("Main.ScanComplete", groups.Count);
            OverallProgress = 100;
            _logService.Info($"Duplicate detection complete, found {groups.Count} groups");
        }
        catch (OperationCanceledException)
        {
            StatusText = _loc.GetString("Main.ScanCancelled");
            _logService.Info("Scan cancelled by user");
        }
        catch (Exception ex)
        {
            StatusText = _loc.GetString("Main.ScanError", ex.Message);
            _logService.Error("Error deleting files", ex);
            System.Windows.MessageBox.Show(
                $"扫描过程中发生错误\n{ex.Message}",
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

    private void OnScanProgress(object? sender, int percent)
        => OverallProgress = percent * 40 / 100;

    /// <summary>停止扫描</summary>
    [RelayCommand]
    private void StopScan() => _scanCts?.Cancel();

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
                _loc.GetString("Delete.NoSelection"),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        // 验证每个组至少保留一个文件
        var violatingGroups = DuplicateGroups
            .Where(g => g.Files.All(f => f.IsSelectedForDeletion))
            .ToList();

        if (violatingGroups.Count > 0)
        {
            System.Windows.MessageBox.Show(
                _loc.GetString("Delete.GroupConstraint"),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var confirmed = Views.DeleteConfirmWindow.ShowConfirm(
            selectedFiles,
            System.Windows.Application.Current.MainWindow);
        if (!confirmed) return;

        try
        {
            StatusText = _loc.GetString("Main.Deleting");

            var progress = new Progress<int>(p => OverallProgress = p);
            var successCount = await _fileOperationService.MoveToRecycleBinAsync(selectedFiles, progress);

            _logService.LogDeletion(selectedFiles.Take(successCount));

            // 从列表中移除已删除的文件并清理空组
            foreach (var group in DuplicateGroups.ToList())
            {
                var toRemove = group.Files.Where(f => f.IsSelectedForDeletion).ToList();
                foreach (var item in toRemove)
                    group.Files.Remove(item);

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
            _logService.Error("Error deleting files", ex);
            System.Windows.MessageBox.Show(
                _loc.GetString("Delete.Failed"),
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            StatusText = _loc.GetString("Main.Ready");
            OverallProgress = 0;
        }
    }

    /// <summary>打开设置窗口</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        var settingsVm = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new Views.SettingsWindow(settingsVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
    }

    /// <summary>预览重复组（由 XAML CommandParameter 传入 DuplicateGroup 实例）</summary>
    [RelayCommand]
    private void PreviewGroup(Models.DuplicateGroup? group)
    {
        if (group == null) return;

        try
        {
            var previewVm = new DuplicatePreviewViewModel(group, _fileOperationService, _loc);
            var previewWindow = new Views.DuplicatePreviewWindow(previewVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logService.Error("Preview failed", ex);
            System.Windows.MessageBox.Show(
                $"预览失败：{ex.Message}",
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>全选推荐删除项（非保留文件设为待删除）</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
                file.IsSelectedForDeletion = !file.IsKeepSuggested;
        }
        UpdateSummary();
    }

    /// <summary>取消所有选中</summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var group in DuplicateGroups)
        foreach (var file in group.Files)
            file.IsSelectedForDeletion = false;

        UpdateSummary();
    }

    /// <summary>导出重复文件信息为 JSON 文件</summary>
    [RelayCommand]
    private void ExportDuplicates()
    {
        if (DuplicateGroups.Count == 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出重复文件信息",
            FileName = $"duplicate{DateTime.Now:yyyyMMddHHmmss}",
            DefaultExt = ".json",
            Filter = "JSON 文件 (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var data = DuplicateGroups.Select(group =>
                group.Files.Select(f => new
                {
                    title = f.FileInfo.Title,
                    fileName = System.IO.Path.GetFileName(f.FileInfo.FilePath),
                    location = f.FileInfo.FilePath,
                    keep = f.IsKeepSuggested,
                    singer = f.FileInfo.Artist,
                    similarity = (int)Math.Round(f.SimilarityScore)
                }).ToArray()
            ).ToArray();

            var json = System.Text.Json.JsonSerializer.Serialize(
                data,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            System.IO.File.WriteAllText(dialog.FileName, json, System.Text.Encoding.UTF8);

            System.Windows.MessageBox.Show(
                $"已成功导出到：\n{dialog.FileName}",
                "导出成功",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.Error("Export duplicates failed", ex);
            System.Windows.MessageBox.Show(
                $"导出失败：{ex.Message}",
                _loc.GetString("App.Title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
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
