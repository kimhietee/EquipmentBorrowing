using Avalonia.Controls;
using EquipmentBorrowing.Desktop.ViewModels;

namespace EquipmentBorrowing.Desktop.Views;

public partial class EquipmentView : UserControl
{
    public EquipmentView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is EquipmentViewModel vm)
                await vm.LoadCommand.ExecuteAsync(null);
        };
    }
}