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
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        // Ensure no file exists at this path
        if (File.Exists(tempPath)) File.Delete(tempPath);

        var settings = AppSettings.Load(tempPath);
        Assert.NotNull(settings);
        Assert.NotEmpty(settings.MusicExtensions);
        Assert.True(settings.IncludeSubdirectories);
        Assert.Equal("zh-CN", settings.Language);
    }

    [Fact]
    public void AppSettings_SaveAndLoad_ShouldPersist()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            var settings = new AppSettings
            {
                Language = "en-US",
                FileNameSimilarityThreshold = 90,
                IncludeSubdirectories = false
            };

            settings.Save(tempPath);
            var loaded = AppSettings.Load(tempPath);

            Assert.Equal("en-US", loaded.Language);
            Assert.Equal(90, loaded.FileNameSimilarityThreshold);
            Assert.False(loaded.IncludeSubdirectories);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
