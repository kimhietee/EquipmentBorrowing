using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class StudentsViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;

    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private string studentNumberInput = string.Empty;
    [ObservableProperty] private string studentNameInput = string.Empty;
    [ObservableProperty] private string? studentContactInput;
    [ObservableProperty] private string selectedEligibilityDisplay = "New student will default to Permitted";

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isUnknownOutcome;

    public StudentsViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value is null)
        {
            SelectedEligibilityDisplay = "New student will default to Permitted";
            return;
        }

        StudentNumberInput = value.StudentNumber;
        StudentNameInput = value.Name;
        StudentContactInput = value.Contact;
        SelectedEligibilityDisplay = value.IsAllowedToBorrow
            ? "Eligibility: Permitted to borrow"
            : "Eligibility: Restricted from borrowing";
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
            var previousId = SelectedStudent?.Id;
            var result = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<Student>>>(
                svc => svc.GetStudentsAsync());

            if (!result.Success)
            {
                Students.Clear();
                HasError = true;
                ErrorMessage = result.Message;
                return;
            }

            Students.Clear();
            foreach (var student in result.Data!)
                Students.Add(student);

            IsEmpty = Students.Count == 0;

            if (previousId.HasValue)
            {
                foreach (var s in Students)
                {
                    if (s.Id == previousId.Value)
                    {
                        SelectedStudent = s;
                        break;
                    }
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterStudentAsync()
    {
        IsUnknownOutcome = false;
        var result = await _scopeRunner.RunAsync<StudentManagementService, ServiceResult<Student>>(
            svc => svc.RegisterStudentAsync(StudentNumberInput, StudentNameInput, StudentContactInput));

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
    private async Task UpdateStudentAsync()
    {
        IsUnknownOutcome = false;
        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student to update.";
            return;
        }

        var studentId = SelectedStudent.Id;
        var result = await _scopeRunner.RunAsync<StudentManagementService, ServiceResult<Student>>(
            svc => svc.UpdateStudentAsync(studentId, StudentNumberInput, StudentNameInput, StudentContactInput));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success || result.Status == OperationStatus.ConflictOrMissing)
            await LoadAsync();
    }

    [RelayCommand]
    private Task PermitStudentAsync() => SetEligibilityAsync(true);

    [RelayCommand]
    private Task RestrictStudentAsync() => SetEligibilityAsync(false);

    private async Task SetEligibilityAsync(bool isAllowed)
    {
        IsUnknownOutcome = false;
        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student first.";
            return;
        }

        var studentId = SelectedStudent.Id;
        var result = await _scopeRunner.RunAsync<StudentManagementService, ServiceResult<Student>>(
            svc => svc.SetBorrowingEligibilityAsync(studentId, isAllowed));

        StatusMessage = result.Message;
        IsUnknownOutcome = result.IsUnknownOutcome;

        if (result.Success || result.Status == OperationStatus.ConflictOrMissing)
            await LoadAsync();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedStudent = null;
        StudentNumberInput = string.Empty;
        StudentNameInput = string.Empty;
        StudentContactInput = null;
        SelectedEligibilityDisplay = "New student will default to Permitted";
    }
}
