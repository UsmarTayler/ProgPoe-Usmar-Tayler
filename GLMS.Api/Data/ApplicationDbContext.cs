using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Data
{
    /// <summary>
    /// The EF Core database context for the GLMS API.
    /// Uses Fluent API to configure relationships, constraints, and keys.
    /// Connection string is stored in appsettings.json (not hardcoded — marking criteria).
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // PRESENTATION POINT: DbSets are how we query the database.
        // Each DbSet<T> maps to a table in SQL Server.
        // _context.Clients.ToListAsync() → SELECT * FROM Clients
        // _context.Contracts.Where(...) → SELECT * FROM Contracts WHERE ...
        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        // PRESENTATION POINT: OnModelCreating uses the Fluent API.
        // This is an alternative to Data Annotations on the models.
        // Fluent API is more powerful — it handles relationships, precision, and naming.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // CLIENT configuration
            // =========================================================
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(c => c.Email)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(c => c.Phone)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(c => c.Region)
                      .IsRequired()
                      .HasMaxLength(100);

                // PRESENTATION POINT: One-to-Many relationship configured with Fluent API.
                // Read as: "A Client HAS MANY Contracts. Each Contract has ONE Client.
                //           The foreign key is ClientId on the Contracts table.
                //           If you try to delete a Client who has Contracts, it BLOCKS the delete
                //           (Restrict) instead of deleting all contracts too (Cascade)."
                entity.HasMany(c => c.Contracts)
                      .WithOne(ct => ct.Client)
                      .HasForeignKey(ct => ct.ClientId)
                      .OnDelete(DeleteBehavior.Restrict); // Protect data integrity
            });

            // =========================================================
            // CONTRACT configuration
            // =========================================================
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(c => c.Id);

                // Store enum as string for readability in the DB
                entity.Property(c => c.Status)
                      .HasConversion<string>()
                      .IsRequired();

                entity.Property(c => c.ServiceLevel)
                      .HasConversion<string>()
                      .IsRequired();

                entity.Property(c => c.ContractType)
                      .HasConversion<string>()
                      .IsRequired();

                entity.Property(c => c.SignedAgreementPath)
                      .HasMaxLength(500);

                entity.Property(c => c.SignedAgreementOriginalName)
                      .HasMaxLength(255);

                // One Contract has many ServiceRequests
                entity.HasMany(c => c.ServiceRequests)
                      .WithOne(sr => sr.Contract)
                      .HasForeignKey(sr => sr.ContractId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // SERVICE REQUEST configuration
            // =========================================================
            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(sr => sr.Id);

                entity.Property(sr => sr.Description)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(sr => sr.CostUSD)
                      .HasColumnType("decimal(18,2)");

                entity.Property(sr => sr.CostZAR)
                      .HasColumnType("decimal(18,2)");

                entity.Property(sr => sr.ExchangeRateUsed)
                      .HasColumnType("decimal(18,4)");

                entity.Property(sr => sr.Status)
                      .HasConversion<string>()
                      .IsRequired();
            });
        }
    }
}
