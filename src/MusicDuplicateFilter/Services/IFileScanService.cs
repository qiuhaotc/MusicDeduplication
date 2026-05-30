using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 文件扫描服务接口
/// </summary>
public interface IFileScanService
{
    /// <summary>扫描进度变化事件（0-100）</summary>
    event EventHandler<int>? ProgressChanged;

    /// <summary>扫描完成事件</summary>
    event EventHandler<List<MusicFileInfo>>? ScanCompleted;

    /// <summary>开始异步扫描指定目录</summary>
    Task<List<MusicFileInfo>> ScanDirectoryAsync(string directory, List<string> extensions, bool includeSubdirectories, CancellationToken cancellationToken = default);
}
