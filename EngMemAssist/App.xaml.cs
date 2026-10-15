using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using EngMemAssist.Data;
using EngMemAssist.Infrastructure;
using EngMemAssist.Integrations;
using EngMemAssist.Navigation;
using EngMemAssist.Nlp;
using EngMemAssist.ViewModels;
using EngMemAssist.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace EngMemAssist;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppPaths.EnsureCreated();

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            await _host.StartAsync();

            _host.Services.GetRequiredService<IDatabaseMigrator>().Migrate();

            ThemeHelper.Apply(_host.Services.GetRequiredService<ISettingsService>().Current.Theme);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

            _host.Services.GetRequiredService<INavigationService>().NavigateTo<HomeViewModel>();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось запустить приложение:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Синхронизация со стартовой таблицей: если таблица не подключена, сначала откроется окно подключения.
        try
        {
            await _host.Services.GetRequiredService<SyncCoordinator>().RunStartupAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске синхронизации:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(AppPaths.SettingsFile));

        services.AddData(AppPaths.DatabaseFile);
        services.AddNlp();
        services.AddIntegrations();

        services.AddSingleton<ISyncLog, FileSyncLog>();
        services.AddSingleton<ILocalSourceService, LocalSourceService>();
        services.AddSingleton<LocalSentenceSearcher>();
        services.AddSingleton<IWordSyncService, WordSyncService>();
        services.AddSingleton<IQuizService, QuizService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<SyncCoordinator>();

        services.AddSingleton<AppBackgroundService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<RecentSessionsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<QuizViewModel>();
        services.AddTransient<ResultsViewModel>();
        services.AddTransient<WordsViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<ConnectSpreadsheetViewModel>();
        services.AddTransient<ConnectSpreadsheetWindow>();
    }
}
