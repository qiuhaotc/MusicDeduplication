namespace MusicDuplicateFilter.Helpers;

/// <summary>
/// 字符串相似度计算工具
/// </summary>
public static class StringSimilarity
{
    /// <summary>
    /// 计算 Levenshtein 距离（编辑距离）
    /// </summary>
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = char.ToLowerInvariant(s[i - 1]) == char.ToLowerInvariant(t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// 计算两个字符串的相似度（0-100），基于 Levenshtein 距离
    /// </summary>
    public static double CalculateSimilarity(string s, string t)
    {
        if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(t)) return 100;
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 0;

        var distance = LevenshteinDistance(s, t);
        var maxLength = Math.Max(s.Length, t.Length);
        return (1.0 - (double)distance / maxLength) * 100;
    }

    /// <summary>
    /// 标准化字符串用于比较：去除特殊字符、多余空格、全部小写
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var result = new System.Text.StringBuilder();
        var lastWasSpace = false;

        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(char.ToLowerInvariant(c));
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                result.Append(' ');
                lastWasSpace = true;
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// 标准化后计算相似度（0-100），忽略大小写和特殊字符
    /// </summary>
    public static double NormalizedSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return 100;
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
        return CalculateSimilarity(Normalize(a), Normalize(b));
    }

    /// <summary>
    /// 提取文件名中可能包含的歌曲信息（去除常见的序号、网站名等噪音）
    /// </summary>
    public static string CleanFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;

        // 去除扩展名
        var name = Path.GetFileNameWithoutExtension(fileName);

        // 去除常见的方括号/圆括号内无关的标签（保留可能包含歌曲信息的内容）
        // 这些是下载站或转码时添加的噪音标签
        var noisePatterns = new[]
        {
            // 匹配 "[Official Video]", "[320 kbps]", "[HQ]", "(Official Video)" 等
            @"[\[\(][^\]\)]*?(?:official\s*(?:music\s*)?video|lyric\s*video|music\s*video)[^\]\)]*?[\]\)]",
            @"[\[\(][^\]\)]*?\d{2,4}\s*kbps[^\]\)]*?[\]\)]",
            @"[\[\(][^\]\)]*?(?:hq|hd)[^\]\)]*?[\]\)]",
            @"[\[\(][^\]\)]*?(?:download|free\s*download)[^\]\)]*?[\]\)]",
            @"[\[\(][^\]\)]*?(?:explicit|clean|dirty)[^\]\)]*?[\]\)]",
            @"[\[\(][^\]\)]*?lyrics[^\]\)]*?[\]\)]",
            // 方括号/圆括号内的 mp3/视频/音频 标签
            @"[\[\(][^\]\)]*?(?:mp3|video|audio|track\s*\d+)[^\]\)]*?[\]\)]",
        };

        var cleaned = name;
        foreach (var pattern in noisePatterns)
        {
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // 再次清理多余的括号（空的方括号或圆括号）
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\[\s*\]", "");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\(\s*\)", "");

        // 合并多余空格
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }
}
