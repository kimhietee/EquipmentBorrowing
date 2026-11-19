using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
