using Models;
using Microsoft.EntityFrameworkCore;

namespace ORM
{
    public class DbManager : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<WaterEntry> WaterEntries { get; set; }
        public DbSet<SleepEntry> SleepEntries { get; set; }


        public DbManager(DbContextOptions<DbManager> options) : base(options)
        {
        }


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


            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();


            modelBuilder.Entity<WaterEntry>()
                .HasOne(w => w.User)
                .WithMany(u => u.WaterEntries)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<WaterEntry>()
                .HasIndex(w => new { w.UserId, w.LoggedAt });


            modelBuilder.Entity<SleepEntry>()
                .HasOne(s => s.User)
                .WithMany(u => u.SleepEntries)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<SleepEntry>()
                .HasIndex(s => new { s.UserId, s.BedTime });
        }
    }
}
