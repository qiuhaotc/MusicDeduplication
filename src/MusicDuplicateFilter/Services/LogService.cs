using System.Collections.Concurrent;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 日志服务实现
/// </summary>
public class LogService : ILogService
{
    private readonly string _logDirectory;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private bool _isWriting;

    public string LogFilePath { get; }

    public LogService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicDuplicateFilter",
            "Logs");

        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);

        LogFilePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }

    public void Info(string message) => Log("INFO", message);

    public void Warn(string message) => Log("WARN", message);

    public void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message} | 异常: {ex.Message}" : message;
        Log("ERROR", fullMessage);
    }

    public void LogDeletion(IEnumerable<string> deletedFiles)
    {
        var fileList = deletedFiles.ToList();
        Info($"批量删除 {fileList.Count} 个文件:");
        foreach (var file in fileList)
            Info($"  - {file}");
    }

    private void Log(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{level}] {message}";

        _logQueue.Enqueue(logEntry);
        _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        if (_isWriting) return;
        _isWriting = true;

        try
        {
            var entries = new List<string>();
            while (_logQueue.TryDequeue(out var entry))
                entries.Add(entry);

            if (entries.Count > 0)
            {
                await File.AppendAllLinesAsync(LogFilePath, entries);
            }
        }
        catch
        {
            // 日志写入失败静默处理
        }
        finally
        {
            _isWriting = false;
        }
    }
}
