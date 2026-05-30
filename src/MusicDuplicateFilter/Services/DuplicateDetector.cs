using MusicDuplicateFilter.Helpers;
using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 重复检测服务实现
/// </summary>
public class DuplicateDetector : IDuplicateDetector
{
    public Task<List<DuplicateGroup>> DetectDuplicatesAsync(
        List<MusicFileInfo> files,
        AppSettings settings,
        IProgress<int>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var groups = new List<DuplicateGroup>();
            var processed = new HashSet<int>(); // 已分配到某个组的文件索引
            var totalPairs = (long)files.Count * (files.Count - 1) / 2;
            var checkedCount = 0L;
            var lastReportedPercent = -1;

            for (var i = 0; i < files.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (processed.Contains(i)) continue;

                var similarFiles = new List<(int index, double score, string type)>();

                for (var j = i + 1; j < files.Count; j++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    checkedCount++;
                    if (totalPairs > 0)
                    {
                        var percent = (int)((double)checkedCount / totalPairs * 100);
                        if (percent != lastReportedPercent)
                        {
                            lastReportedPercent = percent;
                            progressCallback?.Report(percent);
                        }
                    }

                    var score = CalculateDuplicateScore(files[i], files[j], settings, out var similarityType);
                    if (score >= Math.Min(settings.FileNameSimilarityThreshold, settings.MetadataSimilarityThreshold))
                    {
                        similarFiles.Add((j, score, similarityType));
                    }
                }

                if (similarFiles.Count > 0)
                {
                    var group = new DuplicateGroup
                    {
                        SimilarityType = similarFiles.First().type
                    };
                    var maxScore = 0.0;

                    // 添加当前文件
                    var keepItem = new DuplicateFileItem
                    {
                        FileInfo = files[i],
                        IsKeepSuggested = true,
                        IsSelectedForDeletion = false
                    };
                    group.Files.Add(keepItem);
                    processed.Add(i);

                    // 添加所有相似文件
                    foreach (var (index, score, _) in similarFiles)
                    {
                        var item = new DuplicateFileItem
                        {
                            FileInfo = files[index],
                            SimilarityScore = score,
                            IsSelectedForDeletion = true // 默认标记非保留文件为待删除
                        };
                        group.Files.Add(item);
                        processed.Add(index);
                        if (score > maxScore) maxScore = score;
                    }

                    group.MaxScore = maxScore;
                    group.GroupTitle = GenerateGroupTitle(group);
                    groups.Add(group);
                }
            }

            progressCallback?.Report(100);
            return groups;
        }, cancellationToken);
    }

    /// <summary>
    /// 计算两个文件的重复分数
    /// </summary>
    private static double CalculateDuplicateScore(
        MusicFileInfo file1,
        MusicFileInfo file2,
        AppSettings settings,
        out string similarityType)
    {
        var scores = new List<(double score, string type)>();

        // 1. 文件名相似度
        var cleanedName1 = StringSimilarity.CleanFileName(file1.FileName);
        var cleanedName2 = StringSimilarity.CleanFileName(file2.FileName);
        var nameScore = StringSimilarity.CalculateSimilarity(cleanedName1, cleanedName2);
        scores.Add((nameScore, "文件名"));

        // 2. 标题相似度（来自元数据或文件名推断）
        if (!string.IsNullOrEmpty(file1.Title) && !string.IsNullOrEmpty(file2.Title))
        {
            var normalizedTitle1 = StringSimilarity.Normalize(file1.Title);
            var normalizedTitle2 = StringSimilarity.Normalize(file2.Title);
            var titleScore = StringSimilarity.CalculateSimilarity(normalizedTitle1, normalizedTitle2);
            scores.Add((titleScore, "标题"));
        }

        // 3. 艺术家相似度
        if (!string.IsNullOrEmpty(file1.Artist) && !string.IsNullOrEmpty(file2.Artist))
        {
            var normalizedArtist1 = StringSimilarity.Normalize(file1.Artist);
            var normalizedArtist2 = StringSimilarity.Normalize(file2.Artist);
            var artistScore = StringSimilarity.CalculateSimilarity(normalizedArtist1, normalizedArtist2);
            scores.Add((artistScore, "艺术家"));
        }

        // 4. 专辑相似度
        if (!string.IsNullOrEmpty(file1.Album) && !string.IsNullOrEmpty(file2.Album))
        {
            var normalizedAlbum1 = StringSimilarity.Normalize(file1.Album);
            var normalizedAlbum2 = StringSimilarity.Normalize(file2.Album);
            var albumScore = StringSimilarity.CalculateSimilarity(normalizedAlbum1, normalizedAlbum2);
            scores.Add((albumScore, "专辑"));
        }

        // 5. 文件大小相似度
        if (settings.CompareFileSize)
        {
            var sizeDiff = Math.Abs(file1.FileSize - file2.FileSize);
            var sizeScore = sizeDiff <= settings.FileSizeTolerance ? 100.0 :
                100.0 * (1.0 - (double)(sizeDiff - settings.FileSizeTolerance) / Math.Max(file1.FileSize, file2.FileSize));
            sizeScore = Math.Max(0, sizeScore);
            scores.Add((sizeScore, "文件大小"));
        }

        if (scores.Count == 0)
        {
            similarityType = "未知";
            return 0;
        }

        // 加权综合评分：文件名权重最高，元数据次之
        var weightedScore = scores.Average(s => s.score);
        var bestMatch = scores.MaxBy(s => s.score);
        similarityType = bestMatch.type;

        return weightedScore;
    }

    /// <summary>
    /// 生成重复组的标题描述
    /// </summary>
    private static string GenerateGroupTitle(DuplicateGroup group)
    {
        var mainFile = group.Files.FirstOrDefault(f => f.IsKeepSuggested)?.FileInfo;
        if (mainFile == null) return "重复文件组";

        var title = !string.IsNullOrEmpty(mainFile.Title) ? mainFile.Title : StringSimilarity.CleanFileName(mainFile.FileName);
        if (!string.IsNullOrEmpty(mainFile.Artist))
            title = $"{mainFile.Artist} - {title}";

        return title;
    }
}
