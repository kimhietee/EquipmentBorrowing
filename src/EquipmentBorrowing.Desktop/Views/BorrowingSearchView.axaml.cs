using Avalonia.Controls;
using EquipmentBorrowing.Desktop.ViewModels;

namespace EquipmentBorrowing.Desktop.Views;

public partial class BorrowingSearchView : UserControl
{
    public BorrowingSearchView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BorrowingSearchViewModel vm)
                await vm.SearchCommand.ExecuteAsync(null);
        };
    }
}
