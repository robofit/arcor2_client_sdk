using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Services;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Windows;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Pages;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
        .ConfigureServices((context, services) => {
            services.AddHostedService<ApplicationHostService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ITaskBarService, TaskBarService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddTransient<ConnectionPage>();
            services.AddTransient<ConnectionViewModel>();
            services.AddTransient<ScenesPage>();
            services.AddTransient<ScenesViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<SettingsViewModel>();
            

            services.AddSingleton<Arcor2Service>();
        }).Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T GetService<T>()
        where T : class {
        return _host.Services.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e) {
        _host.Start();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e) {
        await _host.StopAsync();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }
}
