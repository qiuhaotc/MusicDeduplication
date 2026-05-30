using MusicDuplicateFilter.Models;
using TagLib;

namespace MusicDuplicateFilter.Helpers;

/// <summary>
/// 音乐文件元数据读取器（基于 TagLibSharp）
/// </summary>
public static class MetadataReader
{
    // TagLib 支持的文件扩展名
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".ogg", ".wma", ".aac", ".m4a", ".ape", ".wv", ".aiff", ".mp4"
    };

    /// <summary>
    /// 读取音乐文件的元数据信息
    /// </summary>
    public static MusicFileInfo ReadMetadata(string filePath, HashSet<string>? allowedExtensions = null)
    {
        var fileInfo = new FileInfo(filePath);
        var musicInfo = new MusicFileInfo
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            Format = fileInfo.Extension.ToLowerInvariant().TrimStart('.'),
        };

        // 尝试从文件名推断标题（元数据不可用时的后备方案）
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        musicInfo.Title = StringSimilarity.CleanFileName(nameWithoutExt);

        // 如果格式不支持 TagLib 读取，跳过
        if (allowedExtensions != null && !allowedExtensions.Contains(fileInfo.Extension))
        {
            return musicInfo;
        }

        if (!SupportedFormats.Contains(fileInfo.Extension))
        {
            return musicInfo;
        }

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var tag = tagFile.Tag;

            // 读取标题
            if (!string.IsNullOrWhiteSpace(tag.Title))
                musicInfo.Title = tag.Title.Trim();

            // 读取艺术家
            if (!string.IsNullOrWhiteSpace(tag.FirstPerformer))
                musicInfo.Artist = tag.FirstPerformer.Trim();
            else if (tag.Performers is { Length: > 0 })
                musicInfo.Artist = string.Join(", ", tag.Performers);

            // 读取专辑
            if (!string.IsNullOrWhiteSpace(tag.Album))
                musicInfo.Album = tag.Album.Trim();

            // 读取时长（Duration 属性返回毫秒数）
            var durationMs = tagFile.Properties.Duration.TotalMilliseconds;
            if (durationMs > 0)
                musicInfo.Duration = TimeSpan.FromMilliseconds(durationMs);
        }
        catch (CorruptFileException)
        {
            // 文件损坏，使用基本信息
        }
        catch (UnsupportedFormatException)
        {
            // 不支持的格式
        }
        catch (Exception)
        {
            // 其他读取错误（如文件被占用等），使用基本信息
        }

        return musicInfo;
    }

    /// <summary>
    /// 检查文件扩展名是否被 TagLib 支持
    /// </summary>
    public static bool IsFormatSupported(string extension)
    {
        return SupportedFormats.Contains(extension);
    }
}
