using Microsoft.EntityFrameworkCore;

namespace Fintable.Data;

public class FintableDb(DbContextOptions<FintableDb> options) : DbContext(options)
{
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<CreditCard> CreditCards { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();

            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Accounts)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();

            entity.HasOne(e => e.Provider)
                .WithMany(p => p.CreditCards)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();

            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Categories)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ExternalId).IsRequired();

            entity.HasOne(e => e.CreditCard)
                .WithMany(c => c.Invoices)
                .HasForeignKey(e => e.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.ExternalId).IsRequired();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(e => e.CreditCard)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(e => new { e.AccountId, e.AccountType });
        });
    }
}
