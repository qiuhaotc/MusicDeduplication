using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 文件操作服务接口
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// 将指定文件移动到回收站
    /// </summary>
    /// <returns>成功移动的文件数量</returns>
    Task<int> MoveToRecycleBinAsync(List<string> filePaths, IProgress<int>? progressCallback = null);

    /// <summary>
    /// 预览文件（使用系统默认程序打开）
    /// </summary>
    void PreviewFile(string filePath);

    /// <summary>
    /// 在文件资源管理器中打开并选中文件
    /// </summary>
    void RevealInExplorer(string filePath);
}
