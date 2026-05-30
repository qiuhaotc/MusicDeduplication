using System.Collections.ObjectModel;

namespace MusicDuplicateFilter.Models;

/// <summary>
/// 扫描结果
/// </summary>
public class ScanResult
{
    /// <summary>扫描的目录</summary>
    public string ScannedDirectory { get; set; } = string.Empty;

    /// <summary>扫描到的音乐文件总数</summary>
    public int TotalFilesScanned { get; set; }

    /// <summary>发现的重复组数量</summary>
    public int DuplicateGroupCount => DuplicateGroups.Count;

    /// <summary>重复文件总数</summary>
    public int DuplicateFileCount => DuplicateGroups.Sum(g => g.FileCount);

    /// <summary>所有重复组</summary>
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; set; } = [];

    /// <summary>可节省的空间（字节）</summary>
    public long PotentialSpaceSaving => DuplicateGroups.Sum(g =>
        g.Files.Where(f => f.IsSelectedForDeletion).Sum(f => f.FileInfo.FileSize));

    /// <summary>可节省的空间（格式化）</summary>
    public string PotentialSpaceSavingDisplay
    {
        get
        {
            var size = PotentialSpaceSaving;
            return size switch
            {
                >= 1_000_000_000 => $"{size / 1_000_000_000.0:F2} GB",
                >= 1_000_000 => $"{size / 1_000_000.0:F2} MB",
                >= 1_000 => $"{size / 1000.0:F2} KB",
                _ => $"{size} B"
            };
        }
    }

    /// <summary>扫描开始时间</summary>
    public DateTime ScanStartTime { get; set; }

    /// <summary>扫描结束时间</summary>
    public DateTime ScanEndTime { get; set; }

    /// <summary>扫描耗时</summary>
    public TimeSpan ScanDuration => ScanEndTime - ScanStartTime;
}
