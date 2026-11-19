using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingSearchViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<BorrowingRecordDetails> Results { get; } = new();

    public IReadOnlyList<BorrowingStatusFilter> StatusOptions { get; } = Enum.GetValues<BorrowingStatusFilter>();
    public IReadOnlyList<BorrowingSortField> SortFieldOptions { get; } = Enum.GetValues<BorrowingSortField>();
    public IReadOnlyList<SortDirection> SortDirectionOptions { get; } = Enum.GetValues<SortDirection>();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private BorrowingStatusFilter selectedStatus = BorrowingStatusFilter.All;
    [ObservableProperty] private BorrowingSortField selectedSortField = BorrowingSortField.DateBorrowed;
    [ObservableProperty] private SortDirection selectedSortDirection = SortDirection.Descending;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEmpty;

    public BorrowingSearchViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        IsEmpty = false;

        try
        {
            var query = new BorrowingSearchQuery(
                SearchText,
                SelectedStatus,
                SelectedSortField,
                SelectedSortDirection);

            var result = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<BorrowingRecordDetails>>>(
                svc => svc.SearchBorrowingsAsync(query, token));

            if (token.IsCancellationRequested)
                return;

            if (!result.Success)
            {
                Results.Clear();
                HasError = true;
                ErrorMessage = result.Message;
                return;
            }

            Results.Clear();
            foreach (var row in result.Data!)
                Results.Add(row);

            IsEmpty = Results.Count == 0;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        SearchText = string.Empty;
        SelectedStatus = BorrowingStatusFilter.All;
        SelectedSortField = BorrowingSortField.DateBorrowed;
        SelectedSortDirection = SortDirection.Descending;
        await SearchAsync();
    }
}
