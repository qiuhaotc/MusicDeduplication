using System.Text.Json;
using System.Text.Json.Serialization;

namespace MusicDuplicateFilter.Models;

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "settings.json");

    /// <summary>扫描目录列表（支持多目录同时扫描）</summary>
    public List<string> ScanDirectories { get; set; } = [];

    /// <summary>支持的音乐文件扩展名</summary>
    public List<string> MusicExtensions { get; set; } = [".mp3", ".flac", ".wav", ".ogg", ".wma", ".aac", ".m4a"];

    /// <summary>文件名相似度阈值（0-100），保留以向后兼容</summary>
    public int FileNameSimilarityThreshold { get; set; } = 80;

    /// <summary>元数据相似度阈值（0-100），保留以向后兼容</summary>
    public int MetadataSimilarityThreshold { get; set; } = 85;

    /// <summary>是否包含子目录扫描</summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>是否比较文件大小</summary>
    public bool CompareFileSize { get; set; } = false;

    /// <summary>文件大小比较容差（字节）</summary>
    public long FileSizeTolerance { get; set; } = 1024 * 100; // 100KB

    /// <summary>扫描并行线程数（1-16，默认4）</summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>默认保留文件策略</summary>
    public KeepPreference KeepPreference { get; set; } = KeepPreference.Largest;

    /// <summary>重复文件匹配规则（权重 + 阈值）</summary>
    public MatchRule MatchRule { get; set; } = new();

    /// <summary>当前语言（zh-CN 或 en-US）</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>是否启用日志</summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>传递性匹配项是否默认勾选删除（默认 true）</summary>
    public bool AutoCheckTransitiveMatches { get; set; } = true;

    /// <summary>上一次窗口位置和大小</summary>
    [JsonIgnore]
    public double WindowLeft { get; set; }

    [JsonIgnore]
    public double WindowTop { get; set; }

    [JsonIgnore]
    public double WindowWidth { get; set; } = 1200;

    [JsonIgnore]
    public double WindowHeight { get; set; } = 800;

    /// <summary>加载设置（自动迁移 LastScanDirectory 到 ScanDirectories）</summary>
    public static AppSettings Load(string? path = null)
    {
        var filePath = path ?? SettingsFilePath;
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                return settings;
            }
        }
        catch
        {
            // 加载失败返回默认设置
        }
        return new AppSettings();
    }

    /// <summary>保存设置</summary>
    public void Save(string? path = null)
    {
        var filePath = path ?? SettingsFilePath;
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // 保存失败静默处理
        }
    }
}
