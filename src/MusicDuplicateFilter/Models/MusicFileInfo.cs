namespace MusicDuplicateFilter.Models;

/// <summary>
/// 音乐文件信息
/// </summary>
public class MusicFileInfo
{
    /// <summary>文件完整路径</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>文件名（不含路径）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>歌曲标题（来自元数据）</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>艺术家</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>专辑</summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>时长</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; set; }

    /// <summary>音频格式（mp3, flac, wav, ogg 等）</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>文件大小格式化显示</summary>
    public string FileSizeDisplay => FileSize switch
    {
        >= 1_000_000_000 => $"{FileSize / 1_000_000_000.0:F2} GB",
        >= 1_000_000 => $"{FileSize / 1_000_000.0:F2} MB",
        >= 1_000 => $"{FileSize / 1000.0:F2} KB",
        _ => $"{FileSize} B"
    };

    /// <summary>时长格式化显示</summary>
    public string DurationDisplay => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");
}
