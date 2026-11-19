using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;

    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);
    [ObservableProperty] private bool showOnlyAvailable;

    [ObservableProperty] private string equipmentIdentifierInput = string.Empty;
    [ObservableProperty] private string equipmentNameInput = string.Empty;
    [ObservableProperty] private string? equipmentDescriptionInput;
    [ObservableProperty] private string selectedAvailabilityDisplay = "New item will default to Available";

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string emptyMessage = "No equipment items in the catalog.";
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isUnknownOutcome;

    public EquipmentViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    partial void OnShowOnlyAvailableChanged(bool value) => _ = LoadAsync();

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value is null)
        {
            SelectedAvailabilityDisplay = "New item will default to Available";
            return;
        }

        EquipmentIdentifierInput = value.Identifier;
        EquipmentNameInput = value.Name;
        EquipmentDescriptionInput = value.Description;
        SelectedAvailabilityDisplay = value.IsAvailable
            ? "Available (managed via borrowing/return)"
            : "Borrowed / Unavailable (managed via borrowing/return)";
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
            var equipmentResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<System.Collections.Generic.List<Equipment>>>(
                svc => svc.GetEquipmentCatalogAsync(ShowOnlyAvailable));

            var studentsResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<System.Collections.Generic.List<Student>>>(
                svc => svc.GetStudentsAsync());

            if (!equipmentResult.Success)
            {
                EquipmentList.Clear();
                HasError = true;
                ErrorMessage = equipmentResult.Message;
                return;
            }

            EquipmentList.Clear();
            foreach (var item in equipmentResult.Data!)
                EquipmentList.Add(item);

            EmptyMessage = ShowOnlyAvailable
                ? "No equipment items are currently available."
                : "No equipment items found in the catalog.";
            IsEmpty = EquipmentList.Count == 0;

            if (studentsResult.Success && studentsResult.Data is not null)
            {
                var previousStudentId = SelectedStudent?.Id;
                Students.Clear();
                foreach (var student in studentsResult.Data)
                    Students.Add(student);

                if (previousStudentId.HasValue)
                {
                    foreach (var s in Students)
                    {
                        if (s.Id == previousStudentId.Value)
                        {
                            SelectedStudent = s;
                            break;
                        }
                    }
                }
            }
            else if (!studentsResult.Success)
            {
                HasError = true;
                ErrorMessage = studentsResult.Message;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        IsUnknownOutcome = false;
        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            return;
        }
        if (SelectedEquipment is null)
        {
            StatusMessage = "Please select an equipment item.";
            return;
        }

        var result = await _scopeRunner.RunAsync<BorrowEquipmentService, BorrowResult>(
            svc => svc.BorrowAsync(
                SelectedStudent.Id,
                SelectedEquipment.Id,
                ExpectedReturnDate?.DateTime));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success || result.Status == OperationStatus.ConflictOrMissing)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task RegisterEquipmentAsync()
    {
        IsUnknownOutcome = false;
        var result = await _scopeRunner.RunAsync<EquipmentManagementService, ServiceResult<Equipment>>(
            svc => svc.RegisterEquipmentAsync(
                EquipmentIdentifierInput,
                EquipmentNameInput,
                EquipmentDescriptionInput));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success)
        {
            ClearForm();
            await LoadAsync();
        }
        else if (result.Status == OperationStatus.ConflictOrMissing)
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task UpdateEquipmentAsync()
    {
        IsUnknownOutcome = false;
        if (SelectedEquipment is null)
        {
            StatusMessage = "Please select an equipment item to update.";
            return;
        }

        var selectedId = SelectedEquipment.Id;
        var result = await _scopeRunner.RunAsync<EquipmentManagementService, ServiceResult<Equipment>>(
            svc => svc.UpdateEquipmentAsync(
                selectedId,
                EquipmentIdentifierInput,
                EquipmentNameInput,
                EquipmentDescriptionInput));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success || result.Status == OperationStatus.ConflictOrMissing)
            await LoadAsync();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedEquipment = null;
        EquipmentIdentifierInput = string.Empty;
        EquipmentNameInput = string.Empty;
        EquipmentDescriptionInput = null;
        SelectedAvailabilityDisplay = "New item will default to Available";
    }
}
