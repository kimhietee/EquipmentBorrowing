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
            var studentRepository = new InMemoryStudentRepository(new[]
            {
                new Student(1, "2026-0001", "Juan Dela Cruz", true),
                new Student(2, "2026-0002", "Maria Santos", true)
            });

            var equipmentRepository = new InMemoryEquipmentRepository(new[]
            {
                new Equipment(101, "Digital Multimeter", "Instrument", true),
                new Equipment(102, "Oscilloscope", "Instrument", true),
                new Equipment(103, "Function Generator", "Instrument", true)
            });

            var borrowingRepository = new InMemoryBorrowingRepository();
            var uow = new InMemoryUnitOfWork();

            var borrowEquipmentService = new BorrowEquipmentService(studentRepository, equipmentRepository, borrowingRepository, uow, 3);
            var returnEquipmentService = new ReturnEquipmentService(borrowingRepository, equipmentRepository, uow);

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