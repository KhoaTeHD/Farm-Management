using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Data
{
    public partial class ApplicationDbContext : DbContext
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

        // Payroll tables
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }
        public DbSet<PayrollEntry> PayrollEntries { get; set; }
        public DbSet<WorkerAdvance> WorkerAdvances { get; set; }
        public DbSet<CropJournalEntry> CropJournalEntries { get; set; }
        public DbSet<CropCareStep> CropCareSteps { get; set; }
        public DbSet<CultivationAudit> CultivationAudits { get; set; }
        public DbSet<InputLot> InputLots { get; set; }
        public DbSet<FieldResource> FieldResources { get; set; }
        public DbSet<InspectionReport> InspectionReports { get; set; }
        public DbSet<HarvestTrace> HarvestTraces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Plant>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<Area>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<PlantType>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<Seed>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<Seed>().HasIndex(e => e.InputLotId);
            modelBuilder.Entity<CropCareStep>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<CropJournalEntry>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<InputLot>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<FieldResource>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<InspectionReport>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<HarvestTrace>().HasQueryFilter(e => e.IsDeleted != true);
            modelBuilder.Entity<CultivationAudit>().HasIndex(e => new { e.RecordType, e.RecordId });
            modelBuilder.Entity<CultivationAudit>().HasIndex(e => new { e.PlantId, e.OccurredAt });
            modelBuilder.Entity<InputLot>().HasIndex(e => e.Code);
            modelBuilder.Entity<InputLot>().Property(e => e.Quantity).HasPrecision(18, 2);
            modelBuilder.Entity<FieldResource>().HasIndex(e => e.AreaId);
            modelBuilder.Entity<InspectionReport>().HasIndex(e => new { e.TargetType, e.TargetId });
            modelBuilder.Entity<HarvestTrace>().HasIndex(e => e.Code);
            modelBuilder.Entity<HarvestTrace>().HasIndex(e => e.JournalEntryId);
            modelBuilder.Entity<HarvestTrace>().HasIndex(e => e.LegacyHarvestId);
            modelBuilder.Entity<HarvestTrace>().Property(e => e.WeightKg).HasPrecision(18, 2);
            modelBuilder.Entity<CropJournalEntry>().HasIndex(e => e.InputLotId);
            modelBuilder.Entity<CropJournalEntry>().HasIndex(e => e.WaterResourceId);

            modelBuilder.Entity<CropJournalEntry>().HasIndex(e => new { e.PlantId, e.Date });
            modelBuilder.Entity<CropJournalEntry>().HasIndex(e => e.CareStepId);
            modelBuilder.Entity<CropJournalEntry>().HasIndex(e => e.RecordCode);
            modelBuilder.Entity<CropCareStep>().HasIndex(e => new { e.PlantId, e.PlannedDay });
            modelBuilder.Entity<CropJournalEntry>().Property(e => e.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<CropJournalEntry>().Property(e => e.HeightCm).HasPrecision(10, 2);
            modelBuilder.Entity<CropJournalEntry>().Property(e => e.SurvivalPercent).HasPrecision(5, 2);
            modelBuilder.Entity<CropJournalEntry>().Property(e => e.AffectedPercent).HasPrecision(5, 2);

            modelBuilder.Entity<WorkerAdvance>().HasIndex(a => a.Reference);
            modelBuilder.Entity<WorkerAdvance>().HasIndex(a => new { a.PayrollYear, a.PayrollMonth, a.WorkerId });
            modelBuilder.Entity<WorkerAdvance>().HasIndex(a => a.WorkerId);
            modelBuilder.Entity<WorkerAdvance>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollRun>().Property(r => r.TotalAdvances).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollRun>().Property(r => r.NetTotal).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.AdvanceAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.NetAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PlantType>().HasIndex(t => t.Name);
            modelBuilder.Entity<Seed>().HasIndex(s => new { s.PlantTypeId, s.Name });

            modelBuilder.Entity<PayrollRun>().HasIndex(r => new { r.Year, r.Month, r.Status });
            modelBuilder.Entity<PayrollRun>().Property(r => r.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollEntry>().HasIndex(e => new { e.PayrollRunId, e.WorkerId });
            modelBuilder.Entity<PayrollEntry>().HasIndex(e => e.WorkerId);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.DailySalary).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.OddHours).HasPrecision(8, 2);
            modelBuilder.Entity<PayrollEntry>().Property(e => e.TotalHours).HasPrecision(8, 2);

            // Lookup indexes; uniqueness is checked by the backend.
            modelBuilder.Entity<Plant>()
                .HasIndex(p => p.BatchCode);

            modelBuilder.Entity<HarvestBatch>()
                .HasIndex(h => h.TraceabilityCode);

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.Code);

            // Decimal precision
            modelBuilder.Entity<Area>()
                .Property(a => a.Acreage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Worker>()
                .Property(w => w.DailySalary)
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

            // Soft references are nullable scalar IDs; navigation properties are NotMapped.
            // Non-unique indexes support explicit lookups without database constraints.
            modelBuilder.Entity<Plant>().HasIndex(x => x.AreaId);
            modelBuilder.Entity<Plant>().HasIndex(x => x.PlantTypeId);
            modelBuilder.Entity<Plant>().HasIndex(x => x.SeedId);
            modelBuilder.Entity<Seed>().HasIndex(x => x.PlantTypeId);
            modelBuilder.Entity<Pesticide>().HasIndex(x => x.PesticideRegistryId);
            modelBuilder.Entity<PesticideApplicationLog>().HasIndex(x => x.PlantId);
            modelBuilder.Entity<PesticideApplicationLog>().HasIndex(x => x.PesticideId);
            modelBuilder.Entity<PesticideApplicationLog>().HasIndex(x => x.WorkerId);
            modelBuilder.Entity<FertilizationLog>().HasIndex(x => x.PlantId);
            modelBuilder.Entity<FertilizationLog>().HasIndex(x => x.FertilizerId);
            modelBuilder.Entity<FertilizationLog>().HasIndex(x => x.WorkerId);
            modelBuilder.Entity<WaterIrrigationLog>().HasIndex(x => x.PlantId);
            modelBuilder.Entity<WaterIrrigationLog>().HasIndex(x => x.WaterSourceId);
            modelBuilder.Entity<WaterIrrigationLog>().HasIndex(x => x.WorkerId);
            modelBuilder.Entity<HarvestBatch>().HasIndex(x => x.PlantId);
            modelBuilder.Entity<HarvestBatch>().HasIndex(x => x.WorkerId);
            modelBuilder.Entity<AttendanceLog>()
                .HasIndex(a => new { a.WorkerId, a.Date });

            // Decimal precision for HoursWorked
            modelBuilder.Entity<AttendanceLog>()
                .Property(a => a.HoursWorked)
                .HasColumnType("decimal(4,2)");
        }
    }
}
