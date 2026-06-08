using BudgetFriend.API.Database.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetFriend.Api.Database.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PasswordHash);

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.IsEmailVerified)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique();
    }
}
