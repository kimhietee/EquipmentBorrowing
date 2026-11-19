using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class EquipmentManagementService
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPersistenceExceptionClassifier _classifier;

    public EquipmentManagementService(
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork,
        IPersistenceExceptionClassifier? classifier = null)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _classifier = classifier ?? DefaultPersistenceExceptionClassifier.Instance;
    }

    public async Task<ServiceResult<Equipment>> RegisterEquipmentAsync(
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return ServiceResult<Equipment>.Validation("Equipment identifier is required.");
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<Equipment>.Validation("Equipment name is required.");

        var normalizedIdentifier = Equipment.NormalizeIdentifier(identifier);

        return await TransactionExecutor.ExecuteWriteAsync<Equipment>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var existing = await _equipmentRepository.FindByNormalizedIdentifierAsync(normalizedIdentifier, ct);
                if (existing is not null)
                    return ServiceResult<Equipment>.Validation($"Equipment identifier '{identifier.Trim()}' already exists.");

                var item = new Equipment(0, identifier, name, description, isAvailable: true);
                await _equipmentRepository.AddAsync(item, ct);
                return ServiceResult<Equipment>.Ok(item, $"Equipment '{item.Name}' ({item.Identifier}) registered as available.");
            },
            duplicateMessage: $"Equipment identifier '{identifier.Trim()}' already exists.",
            concurrencyMessage: "A concurrent update prevented registering the equipment. Please refresh and retry.",
            cancellationToken);
    }

    public async Task<ServiceResult<Equipment>> UpdateEquipmentAsync(
        int equipmentId,
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (equipmentId <= 0)
            return ServiceResult<Equipment>.Validation("Please select an equipment item.");
        if (string.IsNullOrWhiteSpace(identifier))
            return ServiceResult<Equipment>.Validation("Equipment identifier is required.");
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<Equipment>.Validation("Equipment name is required.");

        var normalizedIdentifier = Equipment.NormalizeIdentifier(identifier);

        return await TransactionExecutor.ExecuteWriteAsync<Equipment>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var item = await _equipmentRepository.GetByIdAsync(equipmentId, ct);
                if (item is null)
                    return ServiceResult<Equipment>.Conflict("Selected equipment item no longer exists. Please refresh.");

                var duplicate = await _equipmentRepository.FindByNormalizedIdentifierAsync(normalizedIdentifier, ct);
                if (duplicate is not null && duplicate.Id != equipmentId)
                    return ServiceResult<Equipment>.Validation($"Equipment identifier '{identifier.Trim()}' is already assigned to another item.");

                item.UpdateDetails(identifier, name, description);
                return ServiceResult<Equipment>.Ok(item, $"Equipment '{item.Name}' ({item.Identifier}) updated.");
            },
            duplicateMessage: $"Equipment identifier '{identifier.Trim()}' is already assigned to another item.",
            concurrencyMessage: "Equipment record was modified concurrently. Please refresh and retry.",
            cancellationToken);
    }
}
