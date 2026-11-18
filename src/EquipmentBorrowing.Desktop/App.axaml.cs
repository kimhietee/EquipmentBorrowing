using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Read config: appsettings.json (placeholder), then user-secrets (real password),
            // then environment variables. Later sources override earlier ones.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<App>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

            // Composition root.
            var services = new ServiceCollection();
            services.AddInfrastructure(connectionString);
            services.AddScoped<BorrowEquipmentService>();
            services.AddScoped<ReturnEquipmentService>();
            services.AddScoped<EquipmentViewModel>();
            services.AddScoped<BorrowingsViewModel>();
            services.AddScoped<MainViewModel>();

            // A desktop app has no per-request scope, so one scope lives for the whole app.
            var scope = services.BuildServiceProvider().CreateScope();

            // Apply pending migrations and seed (only if empty) before showing the UI.
            Task.Run(() => InfrastructureSetup.InitializeDatabaseAsync(scope.ServiceProvider))
                .GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = scope.ServiceProvider.GetRequiredService<MainViewModel>(),
            };
            desktop.Exit += (_, _) => scope.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
