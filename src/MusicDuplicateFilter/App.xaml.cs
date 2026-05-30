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

        // 全局异常处理 — 防止未捕获的 Dispatcher 异常静默终止进程
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

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
        LocalizationProvider.Attach(loc);
        var settings = Models.AppSettings.Load();
        loc.SetLanguage(settings.Language);

        // 启动主窗口
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.Exception);
        System.Windows.MessageBox.Show(
            $"发生未处理的错误：\n\n{e.Exception.Message}\n\n详细信息已写入日志文件。",
            "错误",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
        e.Handled = true;   // 阻止进程终止
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogUnhandledException(ex);
    }

    private static void LogUnhandledException(Exception ex)
    {
        try
        {
            var logDir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "crash.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n";
            System.IO.File.AppendAllText(logPath, entry);
        }
        catch { /* 日志写入失败时静默处理 */ }
    }
}

