using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Student?> FindByNormalizedStudentNumberAsync(string normalizedStudentNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
}
