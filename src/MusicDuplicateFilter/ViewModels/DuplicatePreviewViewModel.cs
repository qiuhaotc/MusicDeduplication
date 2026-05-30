using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MusicDuplicateFilter.ViewModels;

/// <summary>
/// 重复文件预览 ViewModel
/// </summary>
public partial class DuplicatePreviewViewModel : ViewModelBase
{
    private readonly Services.IFileOperationService _fileOperationService;
    private readonly Services.ILocalizationService _loc;

    public DuplicatePreviewViewModel(
        Models.DuplicateGroup group,
        Services.IFileOperationService fileOperationService,
        Services.ILocalizationService localizationService)
    {
        _fileOperationService = fileOperationService;
        _loc = localizationService;
        Group = group;

        KeepFile = group.Files.FirstOrDefault(f => f.IsKeepSuggested);
        DuplicateFiles = new System.Collections.ObjectModel.ObservableCollection<Models.DuplicateFileItem>(
            group.Files.Where(f => !f.IsKeepSuggested));
    }

    public Models.DuplicateGroup Group { get; }
    public Models.DuplicateFileItem? KeepFile { get; }
    public System.Collections.ObjectModel.ObservableCollection<Models.DuplicateFileItem> DuplicateFiles { get; }

    private Models.DuplicateFileItem? _selectedDuplicate;
    public Models.DuplicateFileItem? SelectedDuplicate
    {
        get => _selectedDuplicate;
        set => SetProperty(ref _selectedDuplicate, value);
    }

    [RelayCommand]
    private void PlayKeepFile()
    {
        if (KeepFile?.FileInfo?.FilePath != null)
            _fileOperationService.PreviewFile(KeepFile.FileInfo.FilePath);
    }

    [RelayCommand]
    private void PlaySelectedDuplicate()
    {
        if (SelectedDuplicate?.FileInfo?.FilePath != null)
            _fileOperationService.PreviewFile(SelectedDuplicate.FileInfo.FilePath);
    }

    [RelayCommand]
    private void OpenKeepInExplorer()
    {
        if (KeepFile?.FileInfo?.FilePath != null)
            _fileOperationService.RevealInExplorer(KeepFile.FileInfo.FilePath);
    }

    [RelayCommand]
    private void OpenDuplicateInExplorer()
    {
        if (SelectedDuplicate?.FileInfo?.FilePath != null)
            _fileOperationService.RevealInExplorer(SelectedDuplicate.FileInfo.FilePath);
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindowAction?.Invoke();
    }

    public Action? CloseWindowAction { get; set; }
}
