using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly MultiCheckoutViewModel _multiCheckoutViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly BorrowingSearchViewModel _borrowingSearchViewModel;
    private readonly HistoryViewModel _historyViewModel;
    private readonly StudentsViewModel _studentsViewModel;
    private Func<Task<bool>>? _retryInitialization;

    [ObservableProperty] private ViewModelBase currentPage;
    [ObservableProperty] private bool hasStartupWarning;
    [ObservableProperty] private string? startupWarningMessage;

    public MainViewModel(
        EquipmentViewModel equipmentViewModel,
        MultiCheckoutViewModel multiCheckoutViewModel,
        BorrowingsViewModel borrowingsViewModel,
        BorrowingSearchViewModel borrowingSearchViewModel,
        HistoryViewModel historyViewModel,
        StudentsViewModel studentsViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _multiCheckoutViewModel = multiCheckoutViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _borrowingSearchViewModel = borrowingSearchViewModel;
        _historyViewModel = historyViewModel;
        _studentsViewModel = studentsViewModel;
        currentPage = _equipmentViewModel;
    }

    public void ConfigureStartupState(bool initialized, string? warningMessage, Func<Task<bool>> retryInitialization)
    {
        _retryInitialization = retryInitialization;
        HasStartupWarning = !initialized;
        StartupWarningMessage = warningMessage;
    }

    [RelayCommand]
    private async Task RetryDatabaseInitAsync()
    {
        if (_retryInitialization is null)
            return;

        if (await _retryInitialization())
        {
            HasStartupWarning = false;
            StartupWarningMessage = null;
            await RefreshCurrentPageAsync();
        }
        else
        {
            HasStartupWarning = true;
            StartupWarningMessage = "PostgreSQL is still unavailable. Verify the database service is running and try again.";
        }
    }

    private async Task RefreshCurrentPageAsync()
    {
        switch (CurrentPage)
        {
            case EquipmentViewModel vm:
                await vm.LoadCommand.ExecuteAsync(null);
                break;
            case MultiCheckoutViewModel vm:
                await vm.LoadCommand.ExecuteAsync(null);
                break;
            case BorrowingsViewModel vm:
                await vm.LoadCommand.ExecuteAsync(null);
                break;
            case BorrowingSearchViewModel vm:
                await vm.SearchCommand.ExecuteAsync(null);
                break;
            case HistoryViewModel vm:
                await vm.LoadCommand.ExecuteAsync(null);
                break;
            case StudentsViewModel vm:
                await vm.LoadCommand.ExecuteAsync(null);
                break;
        }
    }

    [RelayCommand]
    private void ShowEquipment() => CurrentPage = _equipmentViewModel;

    [RelayCommand]
    private void ShowMultiCheckout() => CurrentPage = _multiCheckoutViewModel;

    [RelayCommand]
    private void ShowBorrowings() => CurrentPage = _borrowingsViewModel;

    [RelayCommand]
    private void ShowSearch() => CurrentPage = _borrowingSearchViewModel;

    [RelayCommand]
    private void ShowHistory() => CurrentPage = _historyViewModel;

    [RelayCommand]
    private void ShowStudents() => CurrentPage = _studentsViewModel;
}
