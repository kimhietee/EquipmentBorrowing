using Avalonia.Controls;
using EquipmentBorrowing.Desktop.ViewModels;

namespace EquipmentBorrowing.Desktop.Views;

public partial class StudentsView : UserControl
{
    public StudentsView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StudentsViewModel vm)
                await vm.LoadCommand.ExecuteAsync(null);
        };
    }
}
