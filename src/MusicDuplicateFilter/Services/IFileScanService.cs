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

    /// <summary>扫描单个目录（向后兼容）</summary>
    Task<List<MusicFileInfo>> ScanDirectoryAsync(string directory, List<string> extensions, bool includeSubdirectories, CancellationToken cancellationToken = default);

    /// <summary>扫描多个目录，支持指定并行线程数</summary>
    Task<List<MusicFileInfo>> ScanDirectoriesAsync(List<string> directories, List<string> extensions, bool includeSubdirectories, int maxParallelism = 4, CancellationToken cancellationToken = default);
}
