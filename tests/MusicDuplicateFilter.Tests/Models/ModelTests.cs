using MusicDuplicateFilter.Models;

namespace MusicDuplicateFilter.Tests.Models;

/// <summary>
/// 数据模型测试
/// </summary>
public class ModelTests
{
    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1500, "1.50 KB")]
    [InlineData(1_500_000, "1.50 MB")]
    [InlineData(1_500_000_000, "1.50 GB")]
    public void FileSizeDisplay_ShouldFormatCorrectly(long size, string expected)
    {
        var info = new MusicFileInfo { FileSize = size };
        Assert.Equal(expected, info.FileSizeDisplay);
    }

    [Fact]
    public void DuplicateGroup_Summary_ShouldShowCountAndSize()
    {
        var group = new DuplicateGroup();
        group.Files.Add(new DuplicateFileItem
        {
            FileInfo = new MusicFileInfo { FileSize = 1_000_000 },
            SimilarityScore = 95
        });
        group.Files.Add(new DuplicateFileItem
        {
            FileInfo = new MusicFileInfo { FileSize = 1_000_000 },
            SimilarityScore = 90
        });

        var summary = group.Summary;
        Assert.Contains("2", summary);
        Assert.Contains("MB", summary);
    }

    [Fact]
    public void ScanResult_PotentialSpaceSaving_ShouldCalculateCorrectly()
    {
        var result = new ScanResult();
        var group = new DuplicateGroup();
        group.Files.Add(new DuplicateFileItem
        {
            FileInfo = new MusicFileInfo { FileSize = 1_000_000 },
            IsSelectedForDeletion = true
        });
        group.Files.Add(new DuplicateFileItem
        {
            FileInfo = new MusicFileInfo { FileSize = 500_000 },
            IsSelectedForDeletion = false
        });

        result.DuplicateGroups.Add(group);
        Assert.Equal(1_000_000, result.PotentialSpaceSaving);
    }

    [Fact]
    public void AppSettings_Load_ShouldReturnDefaultsWhenNoFile()
    {
        // 确保上次测试的保存文件不会影响本测试
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicDuplicateFilter", "settings.json");
        if (File.Exists(settingsPath)) File.Delete(settingsPath);

        var settings = AppSettings.Load();
        Assert.NotNull(settings);
        Assert.NotEmpty(settings.MusicExtensions);
        Assert.True(settings.IncludeSubdirectories);
        Assert.Equal("zh-CN", settings.Language);
    }

    [Fact]
    public void AppSettings_SaveAndLoad_ShouldPersist()
    {
        var settings = new AppSettings
        {
            Language = "en-US",
            FileNameSimilarityThreshold = 90,
            IncludeSubdirectories = false
        };

        // This test verifies the save/load roundtrip
        // Note: Save creates a real file, but it goes to LocalAppData
        settings.Save();
        var loaded = AppSettings.Load();

        // Since Load will load the saved file, values should match
        Assert.Equal("en-US", loaded.Language);
        Assert.Equal(90, loaded.FileNameSimilarityThreshold);
        Assert.False(loaded.IncludeSubdirectories);
    }
}
