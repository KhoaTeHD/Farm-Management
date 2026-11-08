using System.Text.Json;
using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Data;

public partial class ApplicationDbContext
{
    public string? ChangeActor { get; set; }
    public string? ChangeReason { get; set; }
    public string? ExpectedRecordType { get; set; }
    public int? ExpectedRecordId { get; set; }
    public int? ExpectedRevision { get; set; }
    private bool writingAudit;
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (writingAudit) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<CultivationAudit>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Lịch sử được lưu bổ sung; ứng dụng không sửa hoặc xóa các phiên bản đã lưu.");
        var changes = ChangeTracker.Entries().Where(e => e.Entity is ICultivationRecord && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => (Entry: e, Before: e.State == EntityState.Added ? null : JsonSerializer.Serialize(e.OriginalValues.Properties.ToDictionary(p => p.Name, p => e.OriginalValues[p])), Added: e.State == EntityState.Added,
                Deleted: e.State == EntityState.Deleted || (e.Property("IsDeleted").OriginalValue as bool? != true && e.Property("IsDeleted").CurrentValue as bool? == true),
                Restored: e.Property("IsDeleted").OriginalValue as bool? == true && e.Property("IsDeleted").CurrentValue as bool? != true)).ToList();
        if (changes.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (!acceptAllChangesOnSuccess) throw new InvalidOperationException("Audited saves require accepting changes atomically.");
        await using var own = Database.CurrentTransaction == null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            foreach (var c in changes)
            {
                var r = (ICultivationRecord)c.Entry.Entity;
                if (!c.Added && ExpectedRecordType == c.Entry.Metadata.ClrType.Name && ExpectedRecordId == r.Id && ExpectedRevision != (c.Entry.OriginalValues["Revision"] as int? ?? 0))
                    throw new DbUpdateConcurrencyException("Phiên bản hồ sơ đã thay đổi.");
                if (c.Entry.State == EntityState.Deleted) { c.Entry.State = EntityState.Modified; r.IsDeleted = true; }
                if (c.Deleted) r.DeletedAt = DateTime.UtcNow;
                if (c.Restored) r.DeletedAt = null;
                r.Revision = checked((c.Added ? 0 : c.Entry.OriginalValues["Revision"] as int? ?? 0) + 1);
            }
            var count = await base.SaveChangesAsync(true, cancellationToken);
            foreach (var c in changes)
            {
                var r = (ICultivationRecord)c.Entry.Entity;
                var plantId = r is Plant ? r.Id : c.Entry.Metadata.FindProperty("PlantId") != null ? c.Entry.Property("PlantId").CurrentValue as int? : null;
                CultivationAudits.Add(new()
                {
                    RecordType = c.Entry.Metadata.ClrType.Name,
                    RecordId = r.Id,
                    PlantId = plantId,
                    Action = c.Added ? "Created" : c.Deleted ? "Archived" : c.Restored ? "Restored" : "Updated",
                    Actor = ChangeActor,
                    Reason = ChangeReason,
                    OccurredAt = DateTime.UtcNow,
                    BeforeJson = c.Before,
                    AfterJson = JsonSerializer.Serialize(c.Entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => c.Entry.CurrentValues[p]))
                });
            }
            writingAudit = true; await base.SaveChangesAsync(true, cancellationToken); writingAudit = false;
            if (own != null) await own.CommitAsync(cancellationToken);
            return count;
        }
        catch { writingAudit = false; if (own != null) await own.RollbackAsync(cancellationToken); throw; }
    }
}
