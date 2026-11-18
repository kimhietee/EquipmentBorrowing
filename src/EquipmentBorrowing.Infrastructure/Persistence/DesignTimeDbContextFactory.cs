using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        // Creating a migration needs no connection, so the fallback has no password.
        // For "database update", set the env var in your terminal session (see Part 4).
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres";

        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new EquipmentBorrowingDbContext(options);
    }
}