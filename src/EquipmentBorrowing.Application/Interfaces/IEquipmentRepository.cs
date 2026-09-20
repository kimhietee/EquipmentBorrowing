using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
}