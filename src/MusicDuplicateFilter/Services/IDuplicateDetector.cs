using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 重复检测服务接口
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// 检测重复文件组
    /// </summary>
    /// <param name="files">扫描到的所有文件</param>
    /// <param name="settings">应用设置（含阈值）</param>
    /// <param name="progressCallback">进度回调（0-100）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重复组列表</returns>
    Task<List<DuplicateGroup>> DetectDuplicatesAsync(
        List<MusicFileInfo> files,
        AppSettings settings,
        IProgress<int>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
