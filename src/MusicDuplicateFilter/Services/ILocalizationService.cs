namespace MusicDuplicateFilter.Services;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>获取本地化字符串</summary>
    string GetString(string key);

    /// <summary>带格式参数的本地化字符串</summary>
    string GetString(string key, params object[] args);

    /// <summary>当前语言</summary>
    string CurrentLanguage { get; }

    /// <summary>切换语言</summary>
    void SetLanguage(string language);

    /// <summary>支持的语言列表</summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>语言变更事件</summary>
    event EventHandler? LanguageChanged;
}
