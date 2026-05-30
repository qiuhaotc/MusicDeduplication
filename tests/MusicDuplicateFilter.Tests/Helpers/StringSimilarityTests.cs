using MusicDuplicateFilter.Helpers;

namespace MusicDuplicateFilter.Tests.Helpers;

/// <summary>
/// Levenshtein 距离和字符串相似度测试
/// </summary>
public class StringSimilarityTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("flaw", "lawn", 2)]
    [InlineData("Hello", "hello", 0)] // 大小写不敏感
    public void LevenshteinDistance_ShouldReturnCorrectDistance(string s, string t, int expected)
    {
        var result = StringSimilarity.LevenshteinDistance(s, t);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc", "abc", 100)]
    [InlineData("abc", "abd", 66.67)] // 1/3 difference = 66.67%
    [InlineData("abc", "xyz", 0)]
    [InlineData("", "", 100)]
    [InlineData("abc", "", 0)]
    public void CalculateSimilarity_ShouldReturnCorrectScore(string s, string t, double expected)
    {
        var result = StringSimilarity.CalculateSimilarity(s, t);
        Assert.Equal(expected, result, 1); // tolerance of 1
    }

    [Theory]
    [InlineData("Hello World!", "hello world")]
    [InlineData("Artist - Song Name", "artist song name")]
    [InlineData("  Multiple   Spaces  ", "multiple spaces")]
    [InlineData("Song (Official Video)", "song official video")]
    public void Normalize_ShouldRemoveSpecialCharsAndNormalize(string input, string expected)
    {
        var result = StringSimilarity.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Song Name (Official Video)", "Song Name")]
    [InlineData("Artist - Track [320 kbps]", "Artist - Track")]
    [InlineData("Beat - [HQ] Version", "Beat - Version")]
    [InlineData("Simple Song", "Simple Song")]
    [InlineData("K. Williams - 菊次郎的夏天", "K. Williams - 菊次郎的夏天")]
    public void CleanFileName_ShouldRemoveNoise(string fileName, string expected)
    {
        var result = StringSimilarity.CleanFileName(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSimilarity_SimilarFileNames_ShouldReturnHighScore()
    {
        var name1 = "Artist - Song Title";
        var name2 = "Artist - Song Title (Remastered)";
        var score = StringSimilarity.CalculateSimilarity(
            StringSimilarity.Normalize(name1),
            StringSimilarity.Normalize(name2));

        Assert.True(score > 50, $"Expected score > 50, got {score}");
    }

    [Fact]
    public void CalculateSimilarity_DifferentSongs_ShouldReturnLowScore()
    {
        var name1 = "Beatles - Yesterday";
        var name2 = "Queen - Bohemian Rhapsody";
        var score = StringSimilarity.CalculateSimilarity(
            StringSimilarity.Normalize(name1),
            StringSimilarity.Normalize(name2));

        Assert.True(score < 50, $"Expected score < 50, got {score}");
    }
}
