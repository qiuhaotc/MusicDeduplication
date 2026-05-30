namespace MusicDuplicateFilter.Models;

/// <summary>
/// 重复文件匹配规则（权重配置 + 阈值）
/// </summary>
public class MatchRule
{
    /// <summary>文件名相似度权重</summary>
    public double FileNameWeight { get; set; } = 1.0;

    /// <summary>标题元数据相似度权重</summary>
    public double TitleWeight { get; set; } = 1.5;

    /// <summary>艺术家元数据相似度权重</summary>
    public double ArtistWeight { get; set; } = 1.0;

    /// <summary>专辑元数据相似度权重</summary>
    public double AlbumWeight { get; set; } = 0.5;

    /// <summary>时长相似度权重</summary>
    public double DurationWeight { get; set; } = 1.0;

    /// <summary>综合相似度阈值（0-100），超过此值则认为是重复文件</summary>
    public double Threshold { get; set; } = 80.0;

    /// <summary>时长比较容差（秒），差异在此范围内视为相同</summary>
    public double DurationToleranceSeconds { get; set; } = 2.0;

    /// <summary>默认规则</summary>
    public static MatchRule Default => new();
}

/// <summary>
/// 保留文件偏好
/// </summary>
public enum KeepPreference
{
    /// <summary>保留最大文件（默认）</summary>
    Largest,

    /// <summary>保留最小文件</summary>
    Smallest,

    /// <summary>保留元数据最丰富的文件</summary>
    MostMetadata,

    /// <summary>保留最新文件</summary>
    Newest,

    /// <summary>保留最旧文件</summary>
    Oldest
}
