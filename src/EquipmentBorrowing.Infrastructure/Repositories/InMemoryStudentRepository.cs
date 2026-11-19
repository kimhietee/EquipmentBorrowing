using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students;
    private int _nextId;

    public InMemoryStudentRepository(IEnumerable<Student>? seedData = null)
    {
        _students = seedData?.ToList() ?? new List<Student>();
        _nextId = _students.Count > 0 ? _students.Max(s => s.Id) + 1 : 1;
    }

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_students.FirstOrDefault(s => s.Id == id));

    public Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_students.OrderBy(s => s.Name).ThenBy(s => s.Id).ToList());

    public Task<Student?> FindByNormalizedStudentNumberAsync(string normalizedStudentNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_students.FirstOrDefault(s => s.NormalizedStudentNumber == normalizedStudentNumber));

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        if (student.Id == 0)
        {
            typeof(Student).GetProperty(nameof(Student.Id))!.SetValue(student, _nextId++);
        }
        _students.Add(student);
        return Task.CompletedTask;
    }
}
