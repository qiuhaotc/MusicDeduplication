using System.Collections.ObjectModel;

namespace MusicDuplicateFilter.Models;

/// <summary>
/// 一组重复文件
/// </summary>
public class DuplicateGroup
{
    /// <summary>组内所有重复文件</summary>
    public ObservableCollection<DuplicateFileItem> Files { get; set; } = [];

    /// <summary>最高相似度分数（0-100）</summary>
    public double MaxScore { get; set; }

    /// <summary>文件数量</summary>
    public int FileCount => Files.Count;

    /// <summary>组内总大小（字节）</summary>
    public long TotalSize => Files.Sum(f => f.FileInfo.FileSize);

    /// <summary>组内总大小（格式化）</summary>
    public string TotalSizeDisplay
    {
        get
        {
            var size = TotalSize;
            return size switch
            {
                >= 1_000_000_000 => $"{size / 1_000_000_000.0:F2} GB",
                >= 1_000_000 => $"{size / 1_000_000.0:F2} MB",
                >= 1_000 => $"{size / 1000.0:F2} KB",
                _ => $"{size} B"
            };
        }
    }

    /// <summary>摘要描述</summary>
    public string Summary => $"{FileCount} 个重复文件 · {TotalSizeDisplay}";

    /// <summary>组标题（根据相似度类型生成）</summary>
    public string GroupTitle { get; set; } = string.Empty;

    /// <summary>相似度类型描述</summary>
    public string SimilarityType { get; set; } = string.Empty;
}

/// <summary>
/// 重复文件组中的单个文件项
/// </summary>
public class DuplicateFileItem
{
    /// <summary>文件信息</summary>
    public MusicFileInfo FileInfo { get; set; } = null!;

    /// <summary>与组内其它文件的相似度分数（0-100）</summary>
    public double SimilarityScore { get; set; }

    /// <summary>是否被用户选中要删除</summary>
    public bool IsSelectedForDeletion { get; set; }

    /// <summary>是否为该组中建议保留的文件（得分最高或文件质量最好）</summary>
    public bool IsKeepSuggested { get; set; }

    /// <summary>相似度分数显示</summary>
    public string ScoreDisplay => $"{SimilarityScore:F0}%";
}
