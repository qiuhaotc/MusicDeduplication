namespace MusicDuplicateFilter.Services;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogService
{
    /// <summary>记录信息</summary>
    void Info(string message);

    /// <summary>记录警告</summary>
    void Warn(string message);

    /// <summary>记录错误</summary>
    void Error(string message, Exception? ex = null);

    /// <summary>记录删除操作</summary>
    void LogDeletion(IEnumerable<string> deletedFiles);

    /// <summary>获取日志文件路径</summary>
    string LogFilePath { get; }
}
