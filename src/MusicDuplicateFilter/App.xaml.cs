using Microsoft.Extensions.DependencyInjection;
using MusicDuplicateFilter.Services;
using MusicDuplicateFilter.ViewModels;

namespace MusicDuplicateFilter;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置 DI 容器
        var services = new ServiceCollection();

        // 注册服务
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IFileScanService, FileScanService>();
        services.AddSingleton<IDuplicateDetector, DuplicateDetector>();
        services.AddSingleton<IFileOperationService, FileOperationService>();

        // 注册 ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        Services = services.BuildServiceProvider();

        // 初始化本地化
        var loc = Services.GetRequiredService<ILocalizationService>();
        var settings = Models.AppSettings.Load();
        loc.SetLanguage(settings.Language);

        // 启动主窗口
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }
}

