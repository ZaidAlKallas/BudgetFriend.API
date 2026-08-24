using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetFriend.API.Database.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ToAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.HasOne(x => x.FromAccount)
            .WithMany()
            .HasForeignKey(x => x.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToAccount)
            .WithMany()
            .HasForeignKey(x => x.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OutgoingTransaction)
            .WithMany()
            .HasForeignKey(x => x.OutgoingTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IncomingTransaction)
            .WithMany()
            .HasForeignKey(x => x.IncomingTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FromAccountId);
        builder.HasIndex(x => x.ToAccountId);
        builder.HasIndex(x => x.TransferDate);
    }
}
