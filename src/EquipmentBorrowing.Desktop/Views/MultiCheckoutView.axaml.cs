using Avalonia.Controls;
using EquipmentBorrowing.Desktop.ViewModels;

namespace EquipmentBorrowing.Desktop.Views;

public partial class MultiCheckoutView : UserControl
{
    public MultiCheckoutView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is MultiCheckoutViewModel vm)
                await vm.LoadCommand.ExecuteAsync(null);
        };
    }
}
