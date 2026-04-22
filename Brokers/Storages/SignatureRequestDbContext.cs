using DocuSignPoc.Models.Foundations.SignatureRequests;
using Microsoft.EntityFrameworkCore;

namespace DocuSignPoc.Brokers.Storages;

public class SignatureRequestDbContext : DbContext
{
    public SignatureRequestDbContext(DbContextOptions<SignatureRequestDbContext> options)
        : base(options)
    {
    }

    public DbSet<SignatureRequest> SignatureRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SignatureRequest>()
            .ToTable("SignatureRequests")
            .HasKey(s => s.Id);
            
        modelBuilder.Entity<SignatureRequest>()
            .Property(s => s.EnvelopeId)
            .IsRequired(false);
    }
}
