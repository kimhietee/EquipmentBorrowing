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
            // Stable location, so data survives restarts: %LOCALAPPDATA%\EquipmentBorrowing\
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EquipmentBorrowing");
            Directory.CreateDirectory(folder);
            var dbPath = Path.Combine(folder, "equipmentborrowing.db");

            // Composition root.
            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={dbPath}");
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
