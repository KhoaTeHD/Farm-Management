using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class CropJournalController(ApplicationDbContext db, CropSeasonService seasons, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? plantId, int? workerId, string? kind, int? materialItemId)
    {
        var query = db.CropJournalEntries.AsNoTracking();
        if (fromDate.HasValue) query = query.Where(e => e.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(e => e.Date <= toDate.Value.Date);
        if (plantId.HasValue) query = query.Where(e => e.PlantId == plantId.Value);
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(e => e.Kind == kind);
        if (materialItemId.HasValue) query = query.Where(e => e.MaterialItemId == materialItemId.Value);
        if (workerId.HasValue)
        {
            var matchedIds = await db.CropJournalWorkers.AsNoTracking()
                .Where(w => w.WorkerId == workerId.Value && w.CropJournalEntryId.HasValue)
                .Select(w => w.CropJournalEntryId!.Value)
                .Distinct()
                .ToListAsync();
            query = query.Where(e => matchedIds.Contains(e.Id));
        }

        var entries = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        var plants = await db.Plants.AsNoTracking()
            .OrderBy(p => p.Status == "Harvested" || p.Status == "Destroyed" ? 1 : 0)
            .ThenByDescending(p => p.PlantingDate)
            .ToListAsync();
        var plantDict = plants.ToDictionary(p => p.Id);
        var areas = await db.Areas.AsNoTracking().ToDictionaryAsync(a => a.Id);
        var workers = await db.Workers.AsNoTracking().OrderBy(w => w.FullName).ThenBy(w => w.Code).ToListAsync();
        var workerDict = workers.ToDictionary(w => w.Id);
        var materials = await db.MaterialItems.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
        var materialDict = materials.ToDictionary(m => m.Id);

        var entryIds = entries.Select(e => e.Id).ToList();
        var workerLinks = entryIds.Count == 0
            ? new List<CropJournalWorker>()
            : await db.CropJournalWorkers.AsNoTracking()
                .Where(w => w.CropJournalEntryId.HasValue && entryIds.Contains(w.CropJournalEntryId.Value))
                .OrderBy(w => w.Id)
                .ToListAsync();

        var linksByEntry = workerLinks.GroupBy(l => l.CropJournalEntryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<DailyWorkRow>(entries.Count);
        foreach (var e in entries)
        {
            Plant? plant = e.PlantId.HasValue ? plantDict.GetValueOrDefault(e.PlantId.Value) : null;
            string? areaName = plant?.AreaId.HasValue == true ? areas.GetValueOrDefault(plant.AreaId.Value)?.Name : null;

            var workerNames = new List<string>();
            if (linksByEntry.TryGetValue(e.Id, out var links))
            {
                foreach (var link in links)
                {
                    string? name = null;
                    if (link.WorkerId.HasValue && workerDict.TryGetValue(link.WorkerId.Value, out var w))
                        name = !string.IsNullOrWhiteSpace(w.FullName) ? w.FullName : w.Code;
                    else
                        name = link.WorkerName;

                    if (!string.IsNullOrWhiteSpace(name) && !workerNames.Contains(name))
                        workerNames.Add(name);
                }
            }
            if (workerNames.Count == 0 && !string.IsNullOrWhiteSpace(e.Recorder))
                workerNames.Add(e.Recorder);

            string? materialDisplay = e.MaterialItemId.HasValue && materialDict.TryGetValue(e.MaterialItemId.Value, out var mat)
                ? mat.Name
                : e.Material;

            rows.Add(new DailyWorkRow
            {
                Entry = e,
                PlantName = plant?.SeasonName ?? plant?.BatchCode,
                PlantBatchCode = plant?.BatchCode,
                AreaName = areaName,
                WorkerNames = workerNames,
                MaterialDisplay = materialDisplay
            });
        }

        return View(new DailyWorkIndexViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            PlantId = plantId,
            WorkerId = workerId,
            Kind = kind,
            MaterialItemId = materialItemId,
            Plants = plants,
            Workers = workers,
            Materials = materials,
            Items = rows
        });
    }

    public async Task<IActionResult> Create(int? plantId = null, string? kind = null, int? stepId = null, string? returnUrl = null)
    {
        if (plantId.HasValue)
        {
            var plant = await db.Plants.FindAsync(plantId.Value);
            if (plant == null) return NotFound();
        }

        CropCareStep? step = null;
        if (stepId.HasValue)
        {
            if (!plantId.HasValue) return BadRequest("Thiếu thông tin mùa vụ cho việc dự kiến.");
            step = await db.CropCareSteps.FindAsync(stepId.Value);
            if (step == null || step.PlantId != plantId.Value) return BadRequest("Việc dự kiến không thuộc mùa vụ này.");
        }

        var defaultKind = step?.Kind ?? (CropSeasonRules.Kinds.ContainsKey(kind ?? "") ? kind! : "Fertilize");
        var form = new CropJournalForm
        {
            PlantId = plantId,
            Kind = defaultKind,
            CareStepId = stepId,
            Date = DateTime.Today,
            RecordCode = Guid.NewGuid().ToString("N"),
            ReturnUrl = returnUrl
        };
        await Options(form, create: true);
        return View("Form", form);
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var item = await db.CropJournalEntries.FindAsync(id);
        if (item == null) return NotFound();

        var workerIds = await db.CropJournalWorkers.AsNoTracking()
            .Where(w => w.CropJournalEntryId == id && w.WorkerId.HasValue)
            .OrderBy(w => w.Id)
            .Select(w => w.WorkerId!.Value)
            .Distinct()
            .ToListAsync();

        var form = From(item);
        form.WorkerIds = workerIds;
        form.ReturnUrl = returnUrl;
        if (workerIds.Count == 0 && !string.IsNullOrWhiteSpace(item.Recorder))
            form.LegacyRecorder = item.Recorder;
        if (!item.MaterialItemId.HasValue && !string.IsNullOrWhiteSpace(item.Material))
            form.LegacyMaterial = item.Material;

        await Options(form, create: false, existingWorkerIds: workerIds);
        return View("Form", form);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Create(CropJournalForm form)
    {
        form.Id = 0;
        return Save(form, true);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Edit(int id, CropJournalForm form) =>
        id == form.Id ? Save(form, false) : Task.FromResult<IActionResult>(NotFound());

    private async Task<IActionResult> Save(CropJournalForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        var old = create ? new CropJournalEntry() : await db.CropJournalEntries.FindAsync(form.Id);
        if (old == null) return NotFound();

        var existingWorkerIds = create
            ? new List<int>()
            : await db.CropJournalWorkers
                .Where(w => w.CropJournalEntryId == old.Id && w.WorkerId.HasValue)
                .Select(w => w.WorkerId!.Value)
                .ToListAsync();

        var plant = form.PlantId.HasValue ? await db.Plants.FindAsync(form.PlantId.Value) : null;
        if (plant == null)
        {
            ModelState.AddModelError(nameof(form.PlantId), "Không tìm thấy mùa vụ / lô cây.");
        }
        else if (plant.PlantingDate.HasValue && form.Date.HasValue && form.Date.Value.Date < plant.PlantingDate.Value.Date)
        {
            ModelState.AddModelError(nameof(form.Date), "Ngày thực hiện phải từ ngày trồng trở đi.");
        }

        if (!create && old.PlantId.HasValue && old.PlantId != form.PlantId)
            ModelState.AddModelError(nameof(form.PlantId), "Không chuyển công việc sang mùa vụ khác.");

        if (create && await db.CropJournalEntries.AnyAsync(e => e.RecordCode == form.RecordCode))
            ModelState.AddModelError("", "Phiếu này đã được lưu. Quay lại để xem, tránh ghi hai lần.");

        if (form.CareStepId.HasValue)
        {
            var step = await db.CropCareSteps.FindAsync(form.CareStepId.Value);
            if (step == null || step.PlantId != form.PlantId || step.Kind != form.Kind)
                ModelState.AddModelError(nameof(form.CareStepId), "Việc dự kiến không khớp mùa vụ hoặc nhóm công việc.");
        }

        // Kiểm tra danh sách 1..n công nhân
        var selectedWorkerIds = (form.WorkerIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
        var selectedWorkers = new List<Worker>();
        if (selectedWorkerIds.Count == 0)
        {
            ModelState.AddModelError(nameof(form.WorkerIds), "Vui lòng chọn ít nhất 1 công nhân thực hiện.");
        }
        else
        {
            var foundWorkers = await db.Workers.Where(w => selectedWorkerIds.Contains(w.Id)).ToListAsync();
            var workerMap = foundWorkers.ToDictionary(w => w.Id);
            foreach (var wid in selectedWorkerIds)
            {
                if (workerMap.TryGetValue(wid, out var w))
                    selectedWorkers.Add(w);
            }

            if (selectedWorkers.Count != selectedWorkerIds.Count)
            {
                ModelState.AddModelError(nameof(form.WorkerIds), "Có công nhân được chọn không tồn tại.");
            }
            else if (selectedWorkers.Any(w => w.IsActive != true && (create || !existingWorkerIds.Contains(w.Id))))
            {
                ModelState.AddModelError(nameof(form.WorkerIds), "Không thể gán công nhân đã nghỉ việc cho công việc mới.");
            }
        }

        // Kiểm tra vật liệu từ danh mục
        MaterialItem? selectedMaterial = null;
        var usingLegacyMaterial = false;
        if (form.Kind is "Fertilize" or "Pesticide")
        {
            if (form.MaterialItemId.HasValue)
            {
                selectedMaterial = await db.MaterialItems.FindAsync(form.MaterialItemId.Value);
                if (selectedMaterial == null)
                    ModelState.AddModelError(nameof(form.MaterialItemId), "Vật liệu đã chọn không tồn tại.");
                else if (selectedMaterial.IsActive != true && (create || old.MaterialItemId != selectedMaterial.Id))
                    ModelState.AddModelError(nameof(form.MaterialItemId), "Vật liệu đã ngừng sử dụng.");
                else if (!MaterialRules.AllowsKind(selectedMaterial.Category, form.Kind))
                    ModelState.AddModelError(nameof(form.MaterialItemId), "Vật liệu không thuộc nhóm phù hợp với công việc đã chọn.");
            }
            else if (!create && !old.MaterialItemId.HasValue && !string.IsNullOrWhiteSpace(old.Material))
            {
                usingLegacyMaterial = true;
            }
            else
            {
                ModelState.AddModelError(nameof(form.MaterialItemId), "Vui lòng chọn vật liệu từ danh mục.");
            }
        }
        else if (form.Kind == "Care" && form.MaterialItemId.HasValue)
        {
            selectedMaterial = await db.MaterialItems.FindAsync(form.MaterialItemId.Value);
            if (selectedMaterial == null)
                ModelState.AddModelError(nameof(form.MaterialItemId), "Vật liệu đã chọn không tồn tại.");
            else if (selectedMaterial.IsActive != true && (create || old.MaterialItemId != selectedMaterial.Id))
                ModelState.AddModelError(nameof(form.MaterialItemId), "Vật liệu đã ngừng sử dụng.");
        }

        var recorderSummary = selectedWorkers.Count > 0
            ? string.Join(", ", selectedWorkers.Select(w => !string.IsNullOrWhiteSpace(w.FullName) ? w.FullName!.Trim() : (w.Code ?? $"CN #{w.Id}")))
            : form.Recorder?.Trim();
        if (recorderSummary is { Length: > 100 })
            recorderSummary = recorderSummary[..97] + "...";

        var item = new CropJournalEntry
        {
            Id = old.Id,
            PlantId = form.PlantId,
            Revision = old.Revision,
            IsDeleted = old.IsDeleted,
            DeletedAt = old.DeletedAt,
            InputLotId = create ? null : old.InputLotId,
            WaterResourceId = create ? null : old.WaterResourceId,
            CareStepId = form.CareStepId,
            Date = form.Date?.Date,
            Kind = form.Kind,
            RecordCode = create ? form.RecordCode : old.RecordCode ?? form.RecordCode,
            Recorder = recorderSummary,
            Summary = string.IsNullOrWhiteSpace(form.Summary) ? null : form.Summary.Trim(),
            Method = string.IsNullOrWhiteSpace(form.Method) ? null : form.Method.Trim(),
            Weather = string.IsNullOrWhiteSpace(form.Weather) ? null : form.Weather.Trim()
        };

        if (form.Kind is "Water" or "Fertilize" or "Pesticide" or "Care" or "Harvest")
        {
            item.Amount = form.Amount;
            item.Unit = string.IsNullOrWhiteSpace(form.Unit) ? null : form.Unit.Trim();
        }
        if (form.Kind is "Fertilize" or "Pesticide" or "Care")
        {
            if (selectedMaterial != null)
            {
                item.MaterialItemId = selectedMaterial.Id;
                item.Material = selectedMaterial.Name;
            }
            else if (usingLegacyMaterial)
            {
                item.MaterialItemId = null;
                item.Material = old.Material;
            }
        }
        if (form.Kind == "Pesticide") item.WithdrawalDays = form.WithdrawalDays;
        if (form.Kind == "Harvest") item.Quality = string.IsNullOrWhiteSpace(form.Quality) ? null : form.Quality.Trim();
        if (form.Kind == "Observation")
        {
            item.GrowthStage = string.IsNullOrWhiteSpace(form.GrowthStage) ? null : form.GrowthStage;
            item.Condition = string.IsNullOrWhiteSpace(form.Condition) ? null : form.Condition;
            item.HeightCm = form.HeightCm;
            item.SurvivalPercent = form.SurvivalPercent;
            item.AffectedPercent = form.AffectedPercent;
        }

        if (plant != null && (form.Kind is "Pesticide" or "Harvest" || old.Kind == "Pesticide"))
        {
            var harvestDates = await db.CropJournalEntries
                .Where(e => e.PlantId == plant.Id && e.Kind == "Harvest" && e.Id != form.Id)
                .Select(e => e.Date)
                .ToListAsync();
            harvestDates.AddRange(await db.HarvestBatches.Where(h => h.PlantId == plant.Id).Select(h => h.HarvestDate).ToListAsync());
            if (form.Kind == "Harvest") harvestDates.Add(form.Date);

            foreach (var date in harvestDates)
            {
                if (!date.HasValue) continue;
                var safety = await seasons.SafetyAsync(plant.Id, date, create ? 0 : form.Id, item);
                if ((safety.SafeDate.HasValue && safety.SafeDate.Value > date.Value.Date)
                    || (plant.ExpectedSafeHarvestDate.HasValue && plant.ExpectedSafeHarvestDate.Value > date.Value.Date))
                {
                    ModelState.AddModelError("", "Ngày thu hoạch chưa qua mốc cách ly đã ghi nhận.");
                    break;
                }
            }

            if (form.Kind == "Pesticide" && item.Date.HasValue && item.WithdrawalDays.HasValue)
            {
                try { _ = item.Date.Value.AddDays(item.WithdrawalDays.Value); }
                catch (ArgumentOutOfRangeException) { ModelState.AddModelError(nameof(form.WithdrawalDays), "Ngày kết thúc cách ly vượt phạm vi cho phép."); }
            }
        }

        var photo = await ReadPhoto(form.Photo);
        if (!ModelState.IsValid)
        {
            form.ExistingPhoto = old.PhotoPath;
            if (!create && existingWorkerIds.Count == 0 && !string.IsNullOrWhiteSpace(old.Recorder))
                form.LegacyRecorder = old.Recorder;
            if (!create && !old.MaterialItemId.HasValue && !string.IsNullOrWhiteSpace(old.Material))
                form.LegacyMaterial = old.Material;
            await Options(form, create, existingWorkerIds);
            return View("Form", form);
        }

        string? newPath = null;
        try
        {
            if (photo.HasValue)
            {
                newPath = "/uploads/crops/" + Guid.NewGuid().ToString("N") + photo.Value.Extension;
                var directory = Path.Combine(environment.WebRootPath, "uploads", "crops");
                Directory.CreateDirectory(directory);
                await System.IO.File.WriteAllBytesAsync(Path.Combine(directory, Path.GetFileName(newPath)), photo.Value.Bytes);
            }

            item.PhotoPath = newPath ?? (form.RemovePhoto ? null : old.PhotoPath);
            item.CreatedAt = create ? DateTime.Now : old.CreatedAt;
            item.UpdatedAt = DateTime.Now;

            if (create) db.CropJournalEntries.Add(item);
            else db.Entry(old).CurrentValues.SetValues(item);

            await db.SaveChangesAsync();

            // Đồng bộ danh sách công nhân thực hiện trong CropJournalWorkers
            var existingLinks = create
                ? new List<CropJournalWorker>()
                : await db.CropJournalWorkers.Where(w => w.CropJournalEntryId == item.Id).ToListAsync();

            var toRemove = existingLinks.Where(l => !l.WorkerId.HasValue || !selectedWorkerIds.Contains(l.WorkerId.Value)).ToList();
            if (toRemove.Count > 0) db.CropJournalWorkers.RemoveRange(toRemove);

            var keptWorkerIds = existingLinks
                .Where(l => l.WorkerId.HasValue && selectedWorkerIds.Contains(l.WorkerId.Value))
                .Select(l => l.WorkerId!.Value)
                .ToHashSet();

            foreach (var w in selectedWorkers)
            {
                var displayName = !string.IsNullOrWhiteSpace(w.FullName) ? w.FullName : w.Code;
                if (keptWorkerIds.Contains(w.Id))
                {
                    var link = existingLinks.First(l => l.WorkerId == w.Id);
                    link.WorkerName = displayName;
                }
                else
                {
                    db.CropJournalWorkers.Add(new CropJournalWorker
                    {
                        CropJournalEntryId = item.Id,
                        WorkerId = w.Id,
                        WorkerName = displayName
                    });
                }
            }

            await db.SaveChangesAsync();
            await new HarvestTraceService(db).EnsureAsync(item);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            if (newPath != null)
                System.IO.File.Delete(Path.Combine(environment.WebRootPath, "uploads", "crops", Path.GetFileName(newPath)));
            throw;
        }

        TempData["Success"] = "Đã lưu công việc.";
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl))
            return Redirect(form.ReturnUrl);
        return RedirectToAction("Details", "Plants", new { id = item.PlantId });
    }

    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var item = await db.CropJournalEntries.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ReturnUrl = returnUrl;
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        var item = await db.CropJournalEntries.FindAsync(id);
        if (item == null) return NotFound();

        db.CropJournalEntries.Remove(item);
        await db.SaveChangesAsync();
        await new HarvestTraceService(db).EnsureAsync(item);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Success"] = "Đã xóa ghi chép khỏi danh sách đang dùng.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return item.PlantId.HasValue
            ? RedirectToAction("Details", "Plants", new { id = item.PlantId })
            : RedirectToAction(nameof(Index));
    }

    private async Task Options(CropJournalForm form, bool create, IReadOnlyCollection<int>? existingWorkerIds = null)
    {
        var plant = form.PlantId.HasValue ? await db.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == form.PlantId.Value) : null;
        ViewBag.Plant = plant;
        ViewBag.PlantLocked = !create || form.CareStepId.HasValue;
        ViewBag.Plants = await db.Plants.AsNoTracking()
            .OrderBy(p => p.Status == "Harvested" || p.Status == "Destroyed" ? 1 : 0)
            .ThenByDescending(p => p.PlantingDate)
            .ToListAsync();
        ViewBag.PlantTypes = await db.PlantTypes.AsNoTracking().ToDictionaryAsync(t => t.Id);
        ViewBag.Areas = await db.Areas.AsNoTracking().ToDictionaryAsync(a => a.Id);

        ViewBag.Steps = form.PlantId.HasValue && (!create || form.CareStepId.HasValue)
            ? await db.CropCareSteps.AsNoTracking().Where(s => s.PlantId == form.PlantId.Value).OrderBy(s => s.PlannedDay).ToListAsync()
            : await db.CropCareSteps.AsNoTracking().OrderBy(s => s.PlannedDay).ToListAsync();

        ViewBag.Materials = await db.MaterialItems.AsNoTracking()
            .Where(m => m.IsActive == true || (!create && form.MaterialItemId == m.Id))
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();

        var existingSet = existingWorkerIds?.ToHashSet() ?? new HashSet<int>();
        ViewBag.Workers = await db.Workers.AsNoTracking()
            .Where(w => w.IsActive == true || (!create && existingSet.Contains(w.Id)))
            .OrderBy(w => w.FullName)
            .ThenBy(w => w.Code)
            .ToListAsync();
    }

    private async Task<(byte[] Bytes, string Extension)?> ReadPhoto(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(CropJournalForm.Photo), "Ảnh tối đa 5 MB. Hãy chọn ảnh nhỏ hơn.");
            return null;
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();
        string? extension = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension == null)
            ModelState.AddModelError(nameof(CropJournalForm.Photo), "Chỉ nhận ảnh JPG, PNG hoặc WebP; không nhận PDF, SVG hay tệp khác.");
        return extension == null ? null : (bytes, extension);
    }

    private static CropJournalForm From(CropJournalEntry e) => new()
    {
        Id = e.Id,
        PlantId = e.PlantId,
        CareStepId = e.CareStepId,
        MaterialItemId = e.MaterialItemId,
        InputLotId = e.InputLotId,
        WaterResourceId = e.WaterResourceId,
        RecordCode = e.RecordCode ?? Guid.NewGuid().ToString("N"),
        Date = e.Date,
        Kind = e.Kind,
        Recorder = e.Recorder,
        Summary = e.Summary,
        Material = e.Material,
        Amount = e.Amount,
        Unit = e.Unit,
        Method = e.Method,
        Weather = e.Weather,
        WithdrawalDays = e.WithdrawalDays,
        GrowthStage = e.GrowthStage,
        Condition = e.Condition,
        HeightCm = e.HeightCm,
        SurvivalPercent = e.SurvivalPercent,
        AffectedPercent = e.AffectedPercent,
        Quality = e.Quality,
        ExistingPhoto = e.PhotoPath
    };
}
