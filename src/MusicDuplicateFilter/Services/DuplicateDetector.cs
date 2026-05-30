using MusicDuplicateFilter.Helpers;
using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 重复检测服务实现（Bucket + UnionFind + 加权相似度评分）
/// </summary>
public class DuplicateDetector : IDuplicateDetector
{
    public Task<List<DuplicateGroup>> DetectDuplicatesAsync(
        List<MusicFileInfo> files,
        AppSettings settings,
        IProgress<int>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Detect(files, settings, progressCallback, cancellationToken), cancellationToken);
    }

    private static List<DuplicateGroup> Detect(
        List<MusicFileInfo> files,
        AppSettings settings,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        if (files.Count < 2)
        {
            progress?.Report(100);
            return [];
        }

        var rule = settings.MatchRule;

        // ── Step 1: 按粗粒度 Key 分桶，减少 O(n²) 的比较次数 ──
        var buckets = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < files.Count; i++)
        {
            foreach (var key in BucketKeysFor(files[i]))
            {
                if (!buckets.TryGetValue(key, out var list))
                    buckets[key] = list = [];
                list.Add(i);
            }
        }

        // ── Step 2: 对桶内文件两两评分，超过阈值则 Union ──
        var uf = new UnionFind(files.Count);
        var pairScores = new Dictionary<(int, int), double>();
        var processedBuckets = 0;
        var totalBuckets = buckets.Count;

        foreach (var bucket in buckets.Values)
        {
            ct.ThrowIfCancellationRequested();

            var ids = bucket.Distinct().ToArray();
            for (var i = 0; i < ids.Length; i++)
            {
                for (var j = i + 1; j < ids.Length; j++)
                {
                    var a = ids[i]; var b = ids[j];
                    var key = a < b ? (a, b) : (b, a);
                    if (pairScores.ContainsKey(key)) continue;

                    var score = CalculateScore(files[a], files[b], rule);
                    pairScores[key] = score;

                    if (score >= rule.Threshold)
                        uf.Union(a, b);
                }
            }

            processedBuckets++;
            progress?.Report(processedBuckets * 95 / Math.Max(1, totalBuckets));
        }

        // ── Step 3: 按 UnionFind 根节点分组 ──
        var groupMap = new Dictionary<int, List<int>>();
        for (var i = 0; i < files.Count; i++)
        {
            var root = uf.Find(i);
            if (!groupMap.TryGetValue(root, out var list))
                groupMap[root] = list = [];
            list.Add(i);
        }

        // ── Step 4: 构建 DuplicateGroup 列表 ──
        var result = new List<DuplicateGroup>();

        foreach (var (_, members) in groupMap)
        {
            if (members.Count < 2) continue;
            ct.ThrowIfCancellationRequested();

            var keepIdx = DetermineKeepFile(files, members, settings.KeepPreference);
            var minGroupScore = 100.0;
            var group = new DuplicateGroup();

            foreach (var idx in members)
            {
                double score;
                if (idx == keepIdx)
                {
                    score = 100.0;
                }
                else
                {
                    var pKey = idx < keepIdx ? (idx, keepIdx) : (keepIdx, idx);
                    if (!pairScores.TryGetValue(pKey, out score))
                        score = CalculateScore(files[idx], files[keepIdx], rule);
                    if (score < minGroupScore) minGroupScore = score;
                }

                var isTransitive = idx != keepIdx && score < rule.Threshold;
                group.Files.Add(new DuplicateFileItem
                {
                    FileInfo = files[idx],
                    SimilarityScore = score,
                    IsKeepSuggested = idx == keepIdx,
                    IsSelectedForDeletion = idx != keepIdx && (!isTransitive || settings.AutoCheckTransitiveMatches),
                    IsTransitiveMatch = isTransitive
                });
            }

            // 保留文件排最前
            var keepItem = group.Files.FirstOrDefault(f => f.IsKeepSuggested);
            if (keepItem != null && group.Files.IndexOf(keepItem) != 0)
            {
                group.Files.Remove(keepItem);
                group.Files.Insert(0, keepItem);
            }

            group.MaxScore = Math.Round(minGroupScore, 1);
            group.GroupTitle = GenerateGroupTitle(group);
            result.Add(group);
        }

        progress?.Report(100);
        return result
            .OrderByDescending(g => g.MaxScore)
            .ThenByDescending(g => g.FileCount)
            .ToList();
    }

    /// <summary>按加权规则计算两个文件的相似度分数（0-100）</summary>
    public static double CalculateScore(MusicFileInfo a, MusicFileInfo b, MatchRule rule)
    {
        var totalWeight = 0.0;
        var totalScore = 0.0;

        void Add(double weight, double sim)
        {
            if (weight <= 0) return;
            totalWeight += weight;
            totalScore += weight * sim;
        }

        Add(rule.FileNameWeight, StringSimilarity.NormalizedSimilarity(a.CleanedFileName, b.CleanedFileName));

        if (!string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(b.Title))
            Add(rule.TitleWeight, StringSimilarity.NormalizedSimilarity(a.Title, b.Title));

        if (!string.IsNullOrWhiteSpace(a.Artist) && !string.IsNullOrWhiteSpace(b.Artist))
            Add(rule.ArtistWeight, StringSimilarity.NormalizedSimilarity(a.Artist, b.Artist));

        if (!string.IsNullOrWhiteSpace(a.Album) && !string.IsNullOrWhiteSpace(b.Album))
            Add(rule.AlbumWeight, StringSimilarity.NormalizedSimilarity(a.Album, b.Album));

        if (a.Duration > TimeSpan.Zero && b.Duration > TimeSpan.Zero)
        {
            var diff = Math.Abs((a.Duration - b.Duration).TotalSeconds);
            var durSim = diff <= rule.DurationToleranceSeconds
                ? 100.0
                : Math.Max(0, 100.0 - (diff - rule.DurationToleranceSeconds) * 3.0);
            Add(rule.DurationWeight, durSim);
        }

        if (totalWeight <= 0) return 0;
        return Math.Round(totalScore / totalWeight, 2);
    }

    /// <summary>根据保留策略选出要保留的文件下标</summary>
    private static int DetermineKeepFile(List<MusicFileInfo> files, List<int> members, KeepPreference pref)
    {
        return pref switch
        {
            KeepPreference.Largest => members.OrderByDescending(i => files[i].FileSize).First(),
            KeepPreference.Smallest => members.OrderBy(i => files[i].FileSize).First(),
            KeepPreference.MostMetadata => members
                .OrderByDescending(i => MetadataRichness(files[i]))
                .ThenByDescending(i => files[i].FileSize)
                .First(),
            KeepPreference.Newest => members
                .OrderByDescending(i => new FileInfo(files[i].FilePath).LastWriteTime)
                .First(),
            KeepPreference.Oldest => members
                .OrderBy(i => new FileInfo(files[i].FilePath).LastWriteTime)
                .First(),
            _ => members.OrderByDescending(i => files[i].FileSize).First()
        };
    }

    private static int MetadataRichness(MusicFileInfo f)
    {
        var n = 0;
        if (!string.IsNullOrWhiteSpace(f.Title)) n++;
        if (!string.IsNullOrWhiteSpace(f.Artist)) n++;
        if (!string.IsNullOrWhiteSpace(f.Album)) n++;
        if (f.Duration > TimeSpan.Zero) n++;
        return n;
    }

    private static IEnumerable<string> BucketKeysFor(MusicFileInfo f)
    {
        var normName = f.CleanedFileName.ToLowerInvariant().Trim();

        if (normName.Length > 0)
            yield return "n:" + normName[..Math.Min(3, normName.Length)];

        if (!string.IsNullOrWhiteSpace(f.Title))
        {
            var t = f.Title.Trim().ToLowerInvariant();
            yield return "t:" + t[..Math.Min(3, t.Length)];
        }

        if (f.Duration > TimeSpan.Zero)
        {
            var sec = (int)f.Duration.TotalSeconds;
            yield return "d:" + (sec / 5);
        }
    }

    private static string GenerateGroupTitle(DuplicateGroup group)
    {
        var keepFile = group.Files.FirstOrDefault(f => f.IsKeepSuggested)?.FileInfo
                       ?? group.Files.FirstOrDefault()?.FileInfo;
        if (keepFile == null) return "重复文件组";

        var title = !string.IsNullOrWhiteSpace(keepFile.Title)
            ? keepFile.Title
            : keepFile.CleanedFileName;

        return !string.IsNullOrWhiteSpace(keepFile.Artist)
            ? $"{keepFile.Artist} - {title}"
            : title;
    }
}

