using Models;
using Microsoft.EntityFrameworkCore;

namespace ORM
{
    public class DbManager : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<WaterEntry> WaterEntries { get; set; }

        // Constructor für Dependency Injection (WebAPI)
        public DbManager(DbContextOptions<DbManager> options) : base(options)
        {
        }

        // Parameterloser Constructor für Migrations
        public DbManager()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = "Server=localhost;database=healthTracker;user=root";
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User: Unique Constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // WaterEntry: Relationship
            modelBuilder.Entity<WaterEntry>()
                .HasOne(w => w.User)
                .WithMany(u => u.WaterEntries)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WaterEntry: Index für bessere Performance
            modelBuilder.Entity<WaterEntry>()
                .HasIndex(w => new { w.UserId, w.LoggedAt });
        }
    }
}