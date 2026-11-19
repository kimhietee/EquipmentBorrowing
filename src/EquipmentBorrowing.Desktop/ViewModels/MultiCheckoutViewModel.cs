using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MultiCheckoutViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<Equipment> CatalogEquipment { get; } = new();
    public ObservableCollection<Equipment> SelectedItems { get; } = new();

    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private Equipment? equipmentToAdd;
    [ObservableProperty] private Equipment? selectedCheckoutItem;
    [ObservableProperty] private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isUnknownOutcome;

    public MultiCheckoutViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            var studentsResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<Student>>>(
                svc => svc.GetStudentsAsync());
            var equipmentResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<Equipment>>>(
                svc => svc.GetEquipmentCatalogAsync(availableOnly: false));

            if (!studentsResult.Success || !equipmentResult.Success)
            {
                HasError = true;
                ErrorMessage = !studentsResult.Success ? studentsResult.Message : equipmentResult.Message;
                return;
            }

            Students.Clear();
            foreach (var student in studentsResult.Data!)
                Students.Add(student);

            CatalogEquipment.Clear();
            foreach (var item in equipmentResult.Data!)
                CatalogEquipment.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        if (EquipmentToAdd is null)
        {
            StatusMessage = "Please select an equipment item to add.";
            return;
        }

        SelectedItems.Add(EquipmentToAdd);
        StatusMessage = $"Added '{EquipmentToAdd.Identifier} - {EquipmentToAdd.Name}' to checkout list ({SelectedItems.Count} item(s)).";
    }

    [RelayCommand]
    private void RemoveSelectedItem()
    {
        if (SelectedCheckoutItem is null)
            return;

        SelectedItems.Remove(SelectedCheckoutItem);
        SelectedCheckoutItem = null;
    }

    [RelayCommand]
    private void ClearItems()
    {
        SelectedItems.Clear();
        StatusMessage = null;
        IsUnknownOutcome = false;
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        IsUnknownOutcome = false;

        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            return;
        }

        var itemIds = SelectedItems.Select(e => e.Id).ToList();
        var result = await _scopeRunner.RunAsync<BorrowEquipmentService, ServiceResult<IReadOnlyList<Borrowing>>>(
            svc => svc.CheckoutMultipleAsync(
                SelectedStudent.Id,
                itemIds,
                ExpectedReturnDate?.DateTime));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success)
        {
            SelectedItems.Clear();
            await LoadAsync();
        }
        else if (result.Status == OperationStatus.ConflictOrMissing)
        {
            await LoadAsync();
        }
    }
}
