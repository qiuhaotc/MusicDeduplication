using System.Text.Json;
using System.Text.Json.Serialization;

namespace MusicDuplicateFilter.Models;

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MusicDuplicateFilter",
        "settings.json");

    /// <summary>上次扫描的目录</summary>
    public string LastScanDirectory { get; set; } = string.Empty;

    /// <summary>支持的音乐文件扩展名</summary>
    public List<string> MusicExtensions { get; set; } = [".mp3", ".flac", ".wav", ".ogg", ".wma", ".aac", ".m4a"];

    /// <summary>文件名相似度阈值（0-100）</summary>
    public int FileNameSimilarityThreshold { get; set; } = 80;

    /// <summary>元数据相似度阈值（0-100）</summary>
    public int MetadataSimilarityThreshold { get; set; } = 85;

    /// <summary>是否包含子目录扫描</summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>是否比较文件大小</summary>
    public bool CompareFileSize { get; set; } = true;

    /// <summary>文件大小比较容差（字节）</summary>
    public long FileSizeTolerance { get; set; } = 1024 * 100; // 100KB

    /// <summary>当前语言（zh-CN 或 en-US）</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>是否启用日志</summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>上一次窗口位置和大小</summary>
    [JsonIgnore]
    public double WindowLeft { get; set; }

    [JsonIgnore]
    public double WindowTop { get; set; }

    [JsonIgnore]
    public double WindowWidth { get; set; } = 1200;

    [JsonIgnore]
    public double WindowHeight { get; set; } = 800;

    /// <summary>加载设置</summary>
    public static AppSettings Load()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // 加载失败返回默认设置
        }
        return new AppSettings();
    }

    /// <summary>保存设置</summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // 保存失败静默处理
        }
    }
}
