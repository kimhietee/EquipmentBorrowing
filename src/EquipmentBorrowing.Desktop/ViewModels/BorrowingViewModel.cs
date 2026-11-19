using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;

    public ObservableCollection<ActiveBorrowingDetails> ActiveBorrowings { get; } = new();

    [ObservableProperty] private ActiveBorrowingDetails? selectedBorrowing;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isUnknownOutcome;

    public BorrowingsViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        IsEmpty = false;

        try
        {
            var result = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<ActiveBorrowingDetails>>>(
                svc => svc.GetActiveBorrowingsAsync());

            if (!result.Success)
            {
                ActiveBorrowings.Clear();
                HasError = true;
                ErrorMessage = result.Message;
                return;
            }

            ActiveBorrowings.Clear();
            foreach (var item in result.Data!)
                ActiveBorrowings.Add(item);

            IsEmpty = ActiveBorrowings.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        IsUnknownOutcome = false;
        if (SelectedBorrowing is null)
        {
            StatusMessage = "Please select an active borrowing to return.";
            return;
        }

        var borrowingId = SelectedBorrowing.BorrowingId;
        var result = await _scopeRunner.RunAsync<ReturnEquipmentService, ReturnResult>(
            svc => svc.ReturnAsync(borrowingId));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success || result.Status == OperationStatus.ConflictOrMissing)
            await LoadAsync();
    }
}
