using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop.Services;

public interface IOperationScopeRunner
{
    Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> operation)
        where TService : notnull;
}

public sealed class ServiceScopeRunner : IOperationScopeRunner
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceScopeRunner(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> operation)
        where TService : notnull
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await operation(service);
    }
}
