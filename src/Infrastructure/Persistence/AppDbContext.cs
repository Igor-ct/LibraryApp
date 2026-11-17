using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BookRow> Books => Set<BookRow>();
    public DbSet<CopyRow> Copies => Set<CopyRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookRow>(b =>
        {
            b.HasKey(x => x.Isbn);                          
            b.Property(x => x.Isbn).HasMaxLength(20);
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Author).HasMaxLength(100);
            b.Property(x => x.Year).IsRequired();

            b.HasMany(x => x.Copies).WithOne(c => c.Book)
                .HasForeignKey(c => c.BookIsbn)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CopyRow>(c =>
        {
            c.HasKey(x => x.Id);
            c.Property(x => x.Id).HasMaxLength(32);
            c.Property(x => x.BookIsbn).IsRequired().HasMaxLength(20);
            c.Property(x => x.IsIssued).IsRequired();
        });
    }
}