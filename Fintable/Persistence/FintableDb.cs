using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Fintable.Persistence;

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

        var jsonConverter = new ValueConverter<Dictionary<string, string>?, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new()
        );

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Metadata).HasConversion(jsonConverter).HasColumnType("text");
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
            entity.Property(e => e.Kind).IsRequired();
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
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Transactions)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // AccountId is a loose field that can reference either an Account or a CreditCard
            // depending on AccountType; no FK constraint is enforced at the DB level.
            entity.HasIndex(e => new { e.AccountId, e.AccountType });
            entity.HasIndex(e => e.InvoiceId);
        });
    }
}
