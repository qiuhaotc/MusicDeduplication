using MusicDuplicateFilter.Models;
using MusicDuplicateFilter.Services;

namespace MusicDuplicateFilter.Tests.Services;

/// <summary>
/// 重复检测服务测试
/// </summary>
public class DuplicateDetectorTests
{
    private readonly DuplicateDetector _detector = new();

    private static AppSettings CreateSettings(int nameThreshold = 80, int metaThreshold = 85)
    {
        return new AppSettings
        {
            FileNameSimilarityThreshold = nameThreshold,
            MetadataSimilarityThreshold = metaThreshold,
            CompareFileSize = true,
            FileSizeTolerance = 1024 * 100
        };
    }

    [Fact]
    public async Task DetectDuplicates_ExactSameFiles_ShouldDetectAsDuplicate()
    {
        var files = new List<MusicFileInfo>
        {
            new() { FilePath = @"C:\Music\song1.mp3", FileName = "song1.mp3", Title = "My Song", Artist = "Artist A", Album = "Album X", FileSize = 5000 },
            new() { FilePath = @"C:\Music\Copy\song1.mp3", FileName = "song1.mp3", Title = "My Song", Artist = "Artist A", Album = "Album X", FileSize = 5000 },
        };

        var settings = CreateSettings();
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Single(groups);
        Assert.Equal(2, groups[0].FileCount);
        Assert.True(groups[0].MaxScore >= 90);
    }

    [Fact]
    public async Task DetectDuplicates_DifferentSongs_ShouldNotDetect()
    {
        var files = new List<MusicFileInfo>
        {
            new() { FilePath = @"C:\Music\rock.mp3", FileName = "rock.mp3", Title = "Rock Anthem", Artist = "Band X", Album = "Rock Hits", FileSize = 8000 },
            new() { FilePath = @"C:\Music\jazz.mp3", FileName = "jazz.mp3", Title = "Smooth Jazz", Artist = "Artist Y", Album = "Jazz Collection", FileSize = 7000 },
        };

        var settings = CreateSettings();
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task DetectDuplicates_SimilarFileNames_ShouldDetect()
    {
        var files = new List<MusicFileInfo>
        {
            new() { FilePath = @"C:\Music\Beatles - Yesterday.mp3", FileName = "Beatles - Yesterday.mp3", Title = "Yesterday", Artist = "The Beatles", Album = "Help!", FileSize = 6000 },
            new() { FilePath = @"C:\Music\Beatles - Yesterday (Remastered).mp3", FileName = "Beatles - Yesterday (Remastered).mp3", Title = "Yesterday", Artist = "The Beatles", Album = "Help!", FileSize = 6500 },
        };

        var settings = CreateSettings(70, 90);
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Single(groups);
        Assert.Equal(2, groups[0].FileCount);
    }

    [Fact]
    public async Task DetectDuplicates_SameSongDifferentFormat_ShouldDetect()
    {
        var files = new List<MusicFileInfo>
        {
            new() { FilePath = @"C:\Music\song.flac", FileName = "song.flac", Title = "Epic Track", Artist = "DJ Cool", FileSize = 25000 },
            new() { FilePath = @"C:\Music\song.mp3", FileName = "song.mp3", Title = "Epic Track", Artist = "DJ Cool", FileSize = 5000 },
        };

        var settings = CreateSettings(90, 70);
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Single(groups);
    }

    [Fact]
    public async Task DetectDuplicates_EmptyList_ShouldReturnEmpty()
    {
        var files = new List<MusicFileInfo>();
        var settings = CreateSettings();
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task DetectDuplicates_SingleFile_ShouldReturnEmpty()
    {
        var files = new List<MusicFileInfo>
        {
            new() { FilePath = @"C:\Music\only.mp3", FileName = "only.mp3", Title = "Only Song" }
        };

        var settings = CreateSettings();
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task DetectDuplicates_MultipleGroups_ShouldSeparateCorrectly()
    {
        var files = new List<MusicFileInfo>
        {
            // Group 1: duplicates
            new() { FilePath = @"C:\A\song_a.mp3", FileName = "Awesome Song.mp3", Title = "Awesome Song", Artist = "Artist1", FileSize = 5000 },
            new() { FilePath = @"C:\B\song_a_copy.mp3", FileName = "Awesome Song (copy).mp3", Title = "Awesome Song", Artist = "Artist1", FileSize = 5000 },
            // Group 2: duplicates
            new() { FilePath = @"C:\A\other.flac", FileName = "Other Track.flac", Title = "Other Track", Artist = "Artist2", FileSize = 20000 },
            new() { FilePath = @"C:\B\other_remaster.flac", FileName = "Other Track [Remastered].flac", Title = "Other Track", Artist = "Artist2", FileSize = 21000 },
        };

        var settings = CreateSettings(65, 80);
        var groups = await _detector.DetectDuplicatesAsync(files, settings);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(2, g.FileCount));
    }
}
