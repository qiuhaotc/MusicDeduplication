using MusicDuplicateFilter.Helpers;
using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 文件扫描服务实现
/// </summary>
public class FileScanService : IFileScanService
{
    public event EventHandler<int>? ProgressChanged;
    public event EventHandler<List<MusicFileInfo>>? ScanCompleted;

    /// <summary>扫描单个目录（保留旧接口兼容性）</summary>
    public Task<List<MusicFileInfo>> ScanDirectoryAsync(
        string directory,
        List<string> extensions,
        bool includeSubdirectories,
        CancellationToken cancellationToken = default)
        => ScanDirectoriesAsync([directory], extensions, includeSubdirectories, 4, cancellationToken);

    /// <summary>扫描多个目录，支持指定并行线程数</summary>
    public async Task<List<MusicFileInfo>> ScanDirectoriesAsync(
        List<string> directories,
        List<string> extensions,
        bool includeSubdirectories,
        int maxParallelism = 4,
        CancellationToken cancellationToken = default)
    {
        var validDirs = directories
            .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
            .Distinct()
            .ToList();

        if (validDirs.Count == 0)
        {
            ScanCompleted?.Invoke(this, []);
            return [];
        }

        var extensionSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // 收集所有目录下的文件路径
        var files = await Task.Run(() =>
        {
            var allFiles = new List<string>();
            foreach (var dir in validDirs)
            {
                try
                {
                    var found = Directory.EnumerateFiles(dir, "*.*", searchOption)
                        .Where(f => extensionSet.Contains(Path.GetExtension(f)));
                    allFiles.AddRange(found);
                }
                catch (UnauthorizedAccessException) { }
            }
            // 去重（同一文件可能被多个目录路径包含）
            return allFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }, cancellationToken);

        if (files.Count == 0)
        {
            ScanCompleted?.Invoke(this, []);
            return [];
        }

        var results = new List<MusicFileInfo>(files.Count);
        var processedCount = 0;
        var lastReportedPercent = -1;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(maxParallelism, 1, 16),
            CancellationToken = cancellationToken
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(files, parallelOptions, file =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                var musicInfo = MetadataReader.ReadMetadata(file, extensionSet);

                lock (results)
                    results.Add(musicInfo);

                var current = Interlocked.Increment(ref processedCount);
                var percent = (int)((double)current / files.Count * 100);

                if (percent != lastReportedPercent && percent % 5 == 0)
                {
                    lastReportedPercent = percent;
                    ProgressChanged?.Invoke(this, percent);
                }
            });
        }, cancellationToken);

        ProgressChanged?.Invoke(this, 100);
        ScanCompleted?.Invoke(this, results);
        return results;
    }
}
