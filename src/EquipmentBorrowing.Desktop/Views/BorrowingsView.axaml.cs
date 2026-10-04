using Avalonia.Controls;
using EquipmentBorrowing.Desktop.ViewModels;

namespace EquipmentBorrowing.Desktop.Views;

public partial class BorrowingsView : UserControl
{
    public BorrowingsView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BorrowingsViewModel vm)
                await vm.LoadCommand.ExecuteAsync(null);
        };
    }
}  