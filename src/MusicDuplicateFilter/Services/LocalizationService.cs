using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace MusicDuplicateFilter.Services;

/// <summary>
/// 基于 .resx 嵌入资源文件的本地化服务
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
        // 预加载所有语言的资源
        LoadResources("zh-CN", "MusicDuplicateFilter.Resources.Strings.resx");
        LoadResources("en-US", "MusicDuplicateFilter.Resources.Strings.en-US.resx");
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string GetString(string key)
    {
        if (_stringResources.TryGetValue(_currentLanguage, out var resources) &&
            resources.TryGetValue(key, out var value))
        {
            return value;
        }

        // 回退到中文
        if (_currentLanguage != "zh-CN" &&
            _stringResources.TryGetValue("zh-CN", out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return $"[{key}]";
    }

    /// <summary>
    /// 带格式参数的本地化字符串
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void SetLanguage(string language)
    {
        if (_currentLanguage == language) return;

        if (!SupportedLanguages.Contains(language))
            language = "zh-CN";

        _currentLanguage = language;

        // 设置当前线程的 UI 文化
        var culture = new CultureInfo(language);
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 从嵌入的 .resx 文件中加载资源字典
    /// </summary>
    private void LoadResources(string language, string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // 尝试通过部分名称匹配查找资源
                var allResources = assembly.GetManifestResourceNames();
                var matchedName = allResources.FirstOrDefault(r =>
                    r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase) ||
                    r.EndsWith($"Resources.Strings.resx", StringComparison.OrdinalIgnoreCase) && language == "zh-CN" ||
                    r.EndsWith($"Resources.Strings.en-US.resx", StringComparison.OrdinalIgnoreCase) && language == "en-US");

                if (matchedName == null) return;
                using var matchedStream = assembly.GetManifestResourceStream(matchedName);
                if (matchedStream == null) return;
                ParseResxStream(language, matchedStream);
            }
            else
            {
                ParseResxStream(language, stream);
            }
        }
        catch (Exception)
        {
            // 加载失败，静默处理
        }
    }

    private void ParseResxStream(string language, Stream stream)
    {
        var doc = XDocument.Load(stream);
        var resources = new Dictionary<string, string>();

        foreach (var dataElement in doc.Root?.Elements("data") ?? [])
        {
            var name = dataElement.Attribute("name")?.Value;
            var value = dataElement.Element("value")?.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(name))
                resources[name] = value;
        }

        _stringResources[language] = resources;
    }
}
