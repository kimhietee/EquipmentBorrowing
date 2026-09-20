using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;

    [ObservableProperty]
    private ViewModelBase currentPage;

    public MainViewModel(EquipmentViewModel equipmentViewModel, BorrowingsViewModel borrowingsViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        currentPage = _equipmentViewModel;
    }

    [RelayCommand]
    private void ShowEquipment() => CurrentPage = _equipmentViewModel;

    [RelayCommand]
    private void ShowBorrowings() => CurrentPage = _borrowingsViewModel;
}