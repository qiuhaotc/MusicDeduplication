using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 文件操作服务实现
/// </summary>
public class FileOperationService : IFileOperationService
{
    public async Task<int> MoveToRecycleBinAsync(List<string> filePaths, IProgress<int>? progressCallback = null)
    {
        var successCount = 0;
        var total = filePaths.Count;

        for (var i = 0; i < filePaths.Count; i++)
        {
            var filePath = filePaths[i];

            try
            {
                if (!File.Exists(filePath))
                    continue;

                await Task.Run(() => MoveToRecycleBin(filePath));
                successCount++;
            }
            catch (Exception)
            {
                // 删除失败，跳过该文件
            }

            progressCallback?.Report((int)((double)(i + 1) / total * 100));
        }

        return successCount;
    }

    /// <summary>
    /// 使用 Windows Shell API 将文件移动到回收站
    /// </summary>
    private static void MoveToRecycleBin(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 使用 SHFileOperation 或 IFileOperation
            // 这里使用简化的 P/Invoke 方式
            var shf = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = filePath + '\0',
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT
            };

            var result = SHFileOperation(ref shf);
            if (result != 0)
                throw new IOException($"删除文件失败，错误代码: {result}");
        }
        else
        {
            // 非 Windows 平台：直接删除
            File.Delete(filePath);
        }
    }

    public void PreviewFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // 预览失败
        }
    }

    public void RevealInExplorer(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception)
        {
            // 打开失败
        }
    }

    // ---- Windows Shell API P/Invoke ----

    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;
    private const int FOF_NOERRORUI = 0x0400;
    private const int FOF_SILENT = 0x0004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public int wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pTo;
        public int fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
}
