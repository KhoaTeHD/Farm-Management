using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Area> Areas { get; set; }
        public DbSet<PlantType> PlantTypes { get; set; }
        public DbSet<Seed> Seeds { get; set; }
        public DbSet<Plant> Plants { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<PesticideRegistry> PesticideRegistries { get; set; }
        public DbSet<Pesticide> Pesticides { get; set; }
        public DbSet<Fertilizer> Fertilizers { get; set; }
        public DbSet<WaterSource> WaterSources { get; set; }
        public DbSet<PesticideApplicationLog> PesticideApplicationLogs { get; set; }
        public DbSet<FertilizationLog> FertilizationLogs { get; set; }
        public DbSet<WaterIrrigationLog> WaterIrrigationLogs { get; set; }
        public DbSet<HarvestBatch> HarvestBatches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<Plant>()
                .HasIndex(p => p.BatchCode)
                .IsUnique();

            modelBuilder.Entity<HarvestBatch>()
                .HasIndex(h => h.TraceabilityCode)
                .IsUnique();

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.Code)
                .IsUnique();

            // Decimal precision
            modelBuilder.Entity<Area>()
                .Property(a => a.Acreage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Pesticide>()
                .Property(p => p.StockQuantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Fertilizer>()
                .Property(f => f.StockQuantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WaterIrrigationLog>()
                .Property(w => w.WaterAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<HarvestBatch>()
                .Property(h => h.YieldKg)
                .HasColumnType("decimal(18,2)");

            // Configure delete behavior - Restrict để tránh cascade delete phức tạp
            // PesticideApplicationLog
            modelBuilder.Entity<PesticideApplicationLog>()
                .HasOne(l => l.Plant)
                .WithMany(p => p.PesticideApplicationLogs)
                .HasForeignKey(l => l.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PesticideApplicationLog>()
                .HasOne(l => l.Pesticide)
                .WithMany(p => p.PesticideApplicationLogs)
                .HasForeignKey(l => l.PesticideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PesticideApplicationLog>()
                .HasOne(l => l.Worker)
                .WithMany(w => w.PesticideApplicationLogs)
                .HasForeignKey(l => l.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // FertilizationLog
            modelBuilder.Entity<FertilizationLog>()
                .HasOne(l => l.Plant)
                .WithMany(p => p.FertilizationLogs)
                .HasForeignKey(l => l.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FertilizationLog>()
                .HasOne(l => l.Fertilizer)
                .WithMany(f => f.FertilizationLogs)
                .HasForeignKey(l => l.FertilizerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FertilizationLog>()
                .HasOne(l => l.Worker)
                .WithMany(w => w.FertilizationLogs)
                .HasForeignKey(l => l.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // WaterIrrigationLog
            modelBuilder.Entity<WaterIrrigationLog>()
                .HasOne(l => l.Plant)
                .WithMany(p => p.WaterIrrigationLogs)
                .HasForeignKey(l => l.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WaterIrrigationLog>()
                .HasOne(l => l.WaterSource)
                .WithMany(w => w.WaterIrrigationLogs)
                .HasForeignKey(l => l.WaterSourceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WaterIrrigationLog>()
                .HasOne(l => l.Worker)
                .WithMany(w => w.WaterIrrigationLogs)
                .HasForeignKey(l => l.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // HarvestBatch
            modelBuilder.Entity<HarvestBatch>()
                .HasOne(h => h.Plant)
                .WithMany(p => p.HarvestBatches)
                .HasForeignKey(h => h.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HarvestBatch>()
                .HasOne(h => h.Worker)
                .WithMany(w => w.HarvestBatches)
                .HasForeignKey(h => h.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Plant
            modelBuilder.Entity<Plant>()
                .HasOne(p => p.Area)
                .WithMany(a => a.Plants)
                .HasForeignKey(p => p.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plant>()
                .HasOne(p => p.PlantType)
                .WithMany(pt => pt.Plants)
                .HasForeignKey(p => p.PlantTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plant>()
                .HasOne(p => p.Seed)
                .WithMany(s => s.Plants)
                .HasForeignKey(p => p.SeedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pesticide → PesticideRegistry
            modelBuilder.Entity<Pesticide>()
                .HasOne(p => p.PesticideRegistry)
                .WithMany(r => r.Pesticides)
                .HasForeignKey(p => p.PesticideRegistryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed → PlantType
            modelBuilder.Entity<Seed>()
                .HasOne(s => s.PlantType)
                .WithMany(pt => pt.Seeds)
                .HasForeignKey(s => s.PlantTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}