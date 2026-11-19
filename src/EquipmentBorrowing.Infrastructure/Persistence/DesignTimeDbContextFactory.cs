using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("79c0898e-6498-434e-b445-f26d621c2941")
            .Build();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres";

        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new EquipmentBorrowingDbContext(options);
    }
}
