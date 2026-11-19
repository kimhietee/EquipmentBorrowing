using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IOperationScopeRunner _scopeRunner;
    private CancellationTokenSource? _studentHistoryCts;
    private CancellationTokenSource? _equipmentHistoryCts;

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<Equipment> EquipmentItems { get; } = new();
    public ObservableCollection<BorrowingRecordDetails> StudentHistory { get; } = new();
    public ObservableCollection<BorrowingRecordDetails> EquipmentHistory { get; } = new();

    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private Equipment? selectedEquipment;

    [ObservableProperty] private bool isLoadingSubjects;
    [ObservableProperty] private string? subjectErrorMessage;

    [ObservableProperty] private bool isStudentHistoryLoading;
    [ObservableProperty] private bool hasStudentHistoryError;
    [ObservableProperty] private string? studentHistoryErrorMessage;
    [ObservableProperty] private bool isStudentHistoryEmpty;

    [ObservableProperty] private bool isEquipmentHistoryLoading;
    [ObservableProperty] private bool hasEquipmentHistoryError;
    [ObservableProperty] private string? equipmentHistoryErrorMessage;
    [ObservableProperty] private bool isEquipmentHistoryEmpty;

    public HistoryViewModel(IOperationScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner;
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        _ = LoadStudentHistoryAsync();
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        _ = LoadEquipmentHistoryAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoadingSubjects = true;
        SubjectErrorMessage = null;

        try
        {
            var studentsResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<Student>>>(
                svc => svc.GetStudentsAsync());
            var equipmentResult = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<Equipment>>>(
                svc => svc.GetEquipmentCatalogAsync(availableOnly: false));

            if (!studentsResult.Success || !equipmentResult.Success)
            {
                SubjectErrorMessage = !studentsResult.Success ? studentsResult.Message : equipmentResult.Message;
                return;
            }

            var prevStudentId = SelectedStudent?.Id;
            Students.Clear();
            foreach (var student in studentsResult.Data!)
                Students.Add(student);

            var prevEquipId = SelectedEquipment?.Id;
            EquipmentItems.Clear();
            foreach (var item in equipmentResult.Data!)
                EquipmentItems.Add(item);

            if (prevStudentId.HasValue)
            {
                foreach (var s in Students)
                {
                    if (s.Id == prevStudentId.Value)
                    {
                        SelectedStudent = s;
                        break;
                    }
                }
            }

            if (prevEquipId.HasValue)
            {
                foreach (var e in EquipmentItems)
                {
                    if (e.Id == prevEquipId.Value)
                    {
                        SelectedEquipment = e;
                        break;
                    }
                }
            }
        }
        finally
        {
            IsLoadingSubjects = false;
        }
    }

    [RelayCommand]
    private async Task LoadStudentHistoryAsync()
    {
        _studentHistoryCts?.Cancel();
        _studentHistoryCts?.Dispose();
        _studentHistoryCts = new CancellationTokenSource();
        var token = _studentHistoryCts.Token;

        StudentHistory.Clear();
        HasStudentHistoryError = false;
        StudentHistoryErrorMessage = null;
        IsStudentHistoryEmpty = false;

        if (SelectedStudent is null)
            return;

        IsStudentHistoryLoading = true;
        var studentId = SelectedStudent.Id;

        try
        {
            var result = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<BorrowingRecordDetails>>>(
                svc => svc.GetStudentHistoryAsync(studentId, token));

            if (token.IsCancellationRequested)
                return;

            if (!result.Success)
            {
                HasStudentHistoryError = true;
                StudentHistoryErrorMessage = result.Message;
                return;
            }

            foreach (var row in result.Data!)
                StudentHistory.Add(row);

            IsStudentHistoryEmpty = StudentHistory.Count == 0;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsStudentHistoryLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadEquipmentHistoryAsync()
    {
        _equipmentHistoryCts?.Cancel();
        _equipmentHistoryCts?.Dispose();
        _equipmentHistoryCts = new CancellationTokenSource();
        var token = _equipmentHistoryCts.Token;

        EquipmentHistory.Clear();
        HasEquipmentHistoryError = false;
        EquipmentHistoryErrorMessage = null;
        IsEquipmentHistoryEmpty = false;

        if (SelectedEquipment is null)
            return;

        IsEquipmentHistoryLoading = true;
        var equipmentId = SelectedEquipment.Id;

        try
        {
            var result = await _scopeRunner.RunAsync<BorrowingQueryService, ServiceResult<List<BorrowingRecordDetails>>>(
                svc => svc.GetEquipmentHistoryAsync(equipmentId, token));

            if (token.IsCancellationRequested)
                return;

            if (!result.Success)
            {
                HasEquipmentHistoryError = true;
                EquipmentHistoryErrorMessage = result.Message;
                return;
            }

            foreach (var row in result.Data!)
                EquipmentHistory.Add(row);

            IsEquipmentHistoryEmpty = EquipmentHistory.Count == 0;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsEquipmentHistoryLoading = false;
        }
    }
}
