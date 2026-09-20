using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

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
            // Composition root: the only place allowed to construct concrete
            // Infrastructure classes directly (Part H).
            var studentRepository = new InMemoryStudentRepository(new[]
            {
        new Student(1, "Juan Dela Cruz", isAllowedToBorrow: true),
        new Student(2, "Maria Santos", isAllowedToBorrow: true)
    });

            var equipmentRepository = new InMemoryEquipmentRepository(new[]
            {
        new Equipment(101, "Digital Multimeter", isAvailable: true),
        new Equipment(102, "Oscilloscope", isAvailable: true),
        new Equipment(103, "Function Generator", isAvailable: true)
    });

            var borrowingRepository = new InMemoryBorrowingRepository();

            var borrowEquipmentService = new BorrowEquipmentService(studentRepository, equipmentRepository, borrowingRepository, maxActiveBorrowingsPerStudent: 3);
            var returnEquipmentService = new ReturnEquipmentService(borrowingRepository, equipmentRepository);

            var equipmentViewModel = new EquipmentViewModel(equipmentRepository, studentRepository, borrowEquipmentService);
            var borrowingsViewModel = new BorrowingsViewModel(borrowingRepository, returnEquipmentService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(equipmentViewModel, borrowingsViewModel),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}