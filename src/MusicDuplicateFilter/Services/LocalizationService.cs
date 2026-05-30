using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 基于嵌入 JSON 文件的本地化服务
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _stringResources = new();
    private string _currentLanguage = "zh-CN";

    public string CurrentLanguage => _currentLanguage;
    public IReadOnlyList<string> SupportedLanguages { get; } = ["zh-CN", "en-US"];
    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        LoadResources("zh-CN", "pack://application:,,,/Resources/Strings.zh-CN.json");
        LoadResources("en-US", "pack://application:,,,/Resources/Strings.en-US.json");
    }

    /// <summary>获取本地化字符串</summary>
    public string GetString(string key)
    {
        if (_stringResources.TryGetValue(_currentLanguage, out var resources) &&
            resources.TryGetValue(key, out var value))
            return value;

        // 回退到中文
        if (_currentLanguage != "zh-CN" &&
            _stringResources.TryGetValue("zh-CN", out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
            return fallbackValue;

        return key; // 返回 key 本身作为兜底（不加方括号，避免显示丑陋）
    }

    /// <summary>带格式参数的本地化字符串</summary>
    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try { return string.Format(format, args); }
        catch { return format; }
    }

    /// <summary>切换语言</summary>
    public void SetLanguage(string language)
    {
        if (_currentLanguage == language) return;
        if (!SupportedLanguages.Contains(language)) language = "zh-CN";

        _currentLanguage = language;

        var culture = new CultureInfo(language);
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>从 WPF Pack 资源中加载 JSON 本地化文件</summary>
    private void LoadResources(string language, string packUri)
    {
        try
        {
            var uri = new Uri(packUri, UriKind.Absolute);
            var info = System.Windows.Application.GetResourceStream(uri);
            if (info == null) return;

            using var stream = info.Stream;
            var json = new StreamReader(stream).ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null) _stringResources[language] = dict;
        }
        catch
        {
            // 加载失败静默处理
        }
    }
}

/// <summary>
/// 可在 XAML 中通过 {Binding L[Key]} 使用的本地化代理（INotifyPropertyChanged 驱动刷新）
/// </summary>
public sealed class LocalizationProvider : INotifyPropertyChanged
{
    public static LocalizationProvider Current { get; } = new();

    private ILocalizationService? _svc;

    private LocalizationProvider() { }

    /// <summary>绑定到本地化服务（App启动时调用）</summary>
    public static void Attach(ILocalizationService svc)
    {
        Current._svc = svc;
        svc.LanguageChanged += (_, _) =>
            Current.PropertyChanged?.Invoke(Current, new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>XAML 绑定索引器：{Binding L[Main.Browse]}</summary>
    public string this[string key]
    {
        get => _svc?.GetString(key) ?? key;
        // WPF 部分目标属性默认 TwoWay，必须有 setter 否则抛 InvalidOperationException
        set { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

