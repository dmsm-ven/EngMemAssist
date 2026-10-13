using System.Windows;
using EngMemAssist.Core.Settings;
using EngMemAssist.Data;
using EngMemAssist.Navigation;
using EngMemAssist.Nlp;
using EngMemAssist.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<HomeViewModel>();
        services.AddTransient<WordsViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
