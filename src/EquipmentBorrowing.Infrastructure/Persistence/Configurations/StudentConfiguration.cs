using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.StudentNumber).IsRequired().HasMaxLength(32);
        builder.Property(s => s.NormalizedStudentNumber).IsRequired().HasMaxLength(32);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Contact).IsRequired(false).HasMaxLength(100);
        builder.Property(s => s.IsAllowedToBorrow).IsRequired();

        builder.HasIndex(s => s.NormalizedStudentNumber).IsUnique();
        builder.HasIndex(s => s.Name);
    }
}
