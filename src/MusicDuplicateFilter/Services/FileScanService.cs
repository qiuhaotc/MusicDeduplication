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

    public async Task<List<MusicFileInfo>> ScanDirectoryAsync(
        string directory,
        List<string> extensions,
        bool includeSubdirectories,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"目录不存在: {directory}");

        var extensionSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // 先收集所有匹配的文件路径
        var files = await Task.Run(() =>
        {
            var allFiles = new List<string>();
            try
            {
                allFiles = Directory.EnumerateFiles(directory, "*.*", searchOption)
                    .Where(f => extensionSet.Contains(Path.GetExtension(f)))
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                // 跳过无权限的目录
            }
            return allFiles;
        }, cancellationToken);

        if (files.Count == 0)
        {
            ScanCompleted?.Invoke(this, []);
            return [];
        }

        var results = new List<MusicFileInfo>(files.Count);
        var processedCount = 0;
        var lastReportedPercent = -1;

        // 并行处理以加速元数据读取
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
            CancellationToken = cancellationToken
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(files, parallelOptions, file =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                var musicInfo = MetadataReader.ReadMetadata(file, extensionSet);

                lock (results)
                {
                    results.Add(musicInfo);
                }

                var current = Interlocked.Increment(ref processedCount);
                var percent = (int)((double)current / files.Count * 100);

                // 避免频繁触发事件
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
