using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly BorrowEquipmentService _borrowEquipmentService;

    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTimeOffset expectedReturnDate = DateTimeOffset.Now.AddDays(7);
    [ObservableProperty] private string? statusMessage;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowEquipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowEquipmentService = borrowEquipmentService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        EquipmentList.Clear();
        foreach (var item in await _equipmentRepository.GetAllAsync())
            EquipmentList.Add(item);

        Students.Clear();
        foreach (var student in await _studentRepository.GetAllAsync())
            Students.Add(student);
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        if (SelectedStudent is null) { StatusMessage = "Please select a student."; return; }
        if (SelectedEquipment is null) { StatusMessage = "Please select equipment."; return; }

        var result = await _borrowEquipmentService.BorrowAsync(
            SelectedStudent.Id, SelectedEquipment.Id, ExpectedReturnDate.DateTime);

        StatusMessage = result.Message;
        if (result.Success) await LoadAsync();
    }
}