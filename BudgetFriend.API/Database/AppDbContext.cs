using BudgetFriend.API.Database.Entites;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
                    typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
