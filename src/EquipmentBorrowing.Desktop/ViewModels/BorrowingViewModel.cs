using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly ReturnEquipmentService _returnEquipmentService;

    public ObservableCollection<Borrowing> ActiveBorrowings { get; } = new();

    [ObservableProperty] private Borrowing? selectedBorrowing;
    [ObservableProperty] private string? statusMessage;

    public BorrowingsViewModel(IBorrowingRepository borrowingRepository, ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _returnEquipmentService = returnEquipmentService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        ActiveBorrowings.Clear();
        foreach (var borrowing in await _borrowingRepository.GetActiveBorrowingsAsync())
            ActiveBorrowings.Add(borrowing);
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (SelectedBorrowing is null) { StatusMessage = "Please select a borrowing to return."; return; }
        var result = await _returnEquipmentService.ReturnAsync(SelectedBorrowing.Id);
        StatusMessage = result.Message;
        if (result.Success) await LoadAsync();
    }
}