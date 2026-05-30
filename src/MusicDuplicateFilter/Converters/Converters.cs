using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MusicDuplicateFilter.Converters;

/// <summary>
/// 布尔值转可见性
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is true;
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

/// <summary>
/// 相似度分数转颜色
/// </summary>
public class ScoreToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double score) return System.Windows.Media.Brushes.Gray;

        return score switch
        {
            >= 95 => System.Windows.Media.Brushes.Red,
            >= 85 => System.Windows.Media.Brushes.OrangeRed,
            >= 70 => System.Windows.Media.Brushes.DarkOrange,
            _ => System.Windows.Media.Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 文件大小转可读字符串
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long size) return "0 B";

        return size switch
        {
            >= 1_000_000_000 => $"{size / 1_000_000_000.0:F2} GB",
            >= 1_000_000 => $"{size / 1_000_000.0:F2} MB",
            >= 1_000 => $"{size / 1000.0:F2} KB",
            _ => $"{size} B"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Bool 取反转换器
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not bool b || !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not bool b || !b;
    }
}
