using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure;
using Microsoft.Extensions.Configuration;
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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<App>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

            var services = new ServiceCollection();
            services.AddInfrastructure(connectionString);

            // Application services are Scoped so each operation gets its own fresh DbContext.
            services.AddScoped<BorrowEquipmentService>();
            services.AddScoped<ReturnEquipmentService>();
            services.AddScoped<StudentManagementService>();
            services.AddScoped<EquipmentManagementService>();
            services.AddScoped<BorrowingQueryService>();

            // Desktop scope runner + long-lived navigation ViewModels.
            services.AddSingleton<IOperationScopeRunner, ServiceScopeRunner>();
            services.AddSingleton<EquipmentViewModel>();
            services.AddSingleton<MultiCheckoutViewModel>();
            services.AddSingleton<BorrowingsViewModel>();
            services.AddSingleton<BorrowingSearchViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<StudentsViewModel>();
            services.AddSingleton<MainViewModel>();

            var rootProvider = services.BuildServiceProvider();

            bool initialized = Task.Run(() => TryInitializeDatabaseAsync(rootProvider))
                .GetAwaiter().GetResult();

            var mainViewModel = rootProvider.GetRequiredService<MainViewModel>();
            mainViewModel.ConfigureStartupState(
                initialized,
                initialized ? null : "Could not connect to PostgreSQL at startup. Start PostgreSQL and click Retry Connection.",
                () => TryInitializeDatabaseAsync(rootProvider));

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            desktop.Exit += (_, _) => rootProvider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task<bool> TryInitializeDatabaseAsync(IServiceProvider rootProvider)
    {
        try
        {
            await using var scope = rootProvider.CreateAsyncScope();
            await InfrastructureSetup.InitializeDatabaseAsync(scope.ServiceProvider).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
