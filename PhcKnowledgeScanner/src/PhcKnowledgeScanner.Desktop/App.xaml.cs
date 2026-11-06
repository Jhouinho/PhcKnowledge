using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledgeScanner.Application;

namespace PhcKnowledgeScanner.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Information);
            b.AddDebug();
        });
        services.AddPhcKnowledgeScanner();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
