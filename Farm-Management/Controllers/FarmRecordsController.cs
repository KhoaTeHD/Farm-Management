using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class FarmRecordsController(ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Resources = await db.FieldResources.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        ViewBag.Reports = await db.InspectionReports.AsNoTracking().OrderByDescending(r => r.ReportDate).ToListAsync();
        ViewBag.Areas = await db.Areas.AsNoTracking().ToDictionaryAsync(a => a.Id);
        ViewBag.Usage = await db.CropJournalEntries.AsNoTracking().Where(e => e.InputLotId != null).ToListAsync();
        return View(await db.InputLots.AsNoTracking().OrderByDescending(l => l.ReceivedDate).ToListAsync());
    }
    public IActionResult CreateInput() => View("InputForm", new InputLotForm { Code = "VT-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(), ReceivedDate = DateTime.Today });
    public async Task<IActionResult> EditInput(int id)
    {
        var r = await db.InputLots.FindAsync(id); return r == null ? NotFound() : View("InputForm", new InputLotForm { Id = r.Id, Code = r.Code, Kind = r.Kind, ProductName = r.ProductName, ManufacturerBatch = r.ManufacturerBatch, Supplier = r.Supplier, InvoiceNumber = r.InvoiceNumber, ReceivedDate = r.ReceivedDate, ExpiryDate = r.ExpiryDate, Quantity = r.Quantity, Unit = r.Unit, RegistrationNumber = r.RegistrationNumber, Status = r.Status, Notes = r.Notes });
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> CreateInput(InputLotForm f) { f.Id = 0; return SaveInput(f, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> EditInput(int id, InputLotForm f) => id == f.Id ? SaveInput(f, false) : Task.FromResult<IActionResult>(NotFound());
    private async Task<IActionResult> SaveInput(InputLotForm f, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var r = create ? new InputLot() : await db.InputLots.FindAsync(f.Id); if (r == null) return NotFound();
        f.Code = f.Code?.Trim().ToUpperInvariant();
        if (await db.InputLots.IgnoreQueryFilters().AnyAsync(l => l.Id != f.Id && l.Code == f.Code)) ModelState.AddModelError(nameof(f.Code), "Mã đã tồn tại, kể cả trong hồ sơ lưu trữ.");
        var used = await db.CropJournalEntries.Where(e => e.InputLotId == f.Id && !create).ToListAsync();
        if (used.Any() && (r.Kind != f.Kind || r.Unit != f.Unit)) ModelState.AddModelError("", "Lô đã sử dụng: không đổi nhóm hoặc đơn vị gốc; tạo lô mới khi cần.");
        var quantities = used.Select(e => FarmRecordRules.ConvertAmount(e.Amount, e.Unit, f.Unit)).ToList();
        if (quantities.Any(q => q == null) || quantities.Sum() > f.Quantity) ModelState.AddModelError(nameof(f.Quantity), "Số lượng nhận không được nhỏ hơn lượng đã ghi sử dụng hoặc có lượng dùng chưa rõ đơn vị.");
        if (used.Any(e => e.Date == null || e.Date < f.ReceivedDate || (f.ExpiryDate.HasValue && e.Date > f.ExpiryDate))) ModelState.AddModelError("", "Ngày nhận/hạn dùng mới mâu thuẫn nhật ký sử dụng đã lưu.");
        if (f.Status == "Released" && !await FarmRecordRules.TestsAllowAsync(db, "InputLot", r.Id)) ModelState.AddModelError(nameof(f.Status), "Còn chỉ tiêu chưa đạt/chưa có kết luận. Bổ sung kết quả khắc phục trước khi cho dùng.");
        if (!ModelState.IsValid) return View("InputForm", f);
        r.Code = f.Code; r.Kind = f.Kind; r.ProductName = f.ProductName?.Trim(); r.ManufacturerBatch = f.ManufacturerBatch; r.Supplier = f.Supplier; r.InvoiceNumber = f.InvoiceNumber; r.ReceivedDate = f.ReceivedDate?.Date; r.ExpiryDate = f.ExpiryDate?.Date; r.Quantity = f.Quantity; r.Unit = f.Unit; r.RegistrationNumber = f.RegistrationNumber; r.Status = f.Status; r.Notes = f.Notes;
        if (create) db.InputLots.Add(r); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu hồ sơ lô vật tư."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> CreateResource() { await Areas(); return View("ResourceForm", new ResourceForm()); }
    public async Task<IActionResult> EditResource(int id)
    { var r = await db.FieldResources.FindAsync(id); if (r == null) return NotFound(); await Areas(); return View("ResourceForm", new ResourceForm { Id = r.Id, AreaId = r.AreaId, Kind = r.Kind, Name = r.Name, Location = r.Location, Status = r.Status, RiskAssessment = r.RiskAssessment, Treatment = r.Treatment }); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> CreateResource(ResourceForm f) { f.Id = 0; return SaveResource(f, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> EditResource(int id, ResourceForm f) => id == f.Id ? SaveResource(f, false) : Task.FromResult<IActionResult>(NotFound());
    private async Task<IActionResult> SaveResource(ResourceForm f, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var r = create ? new FieldResource() : await db.FieldResources.FindAsync(f.Id); if (r == null) return NotFound();
        if (!await db.Areas.AnyAsync(a => a.Id == f.AreaId)) ModelState.AddModelError(nameof(f.AreaId), "Vùng trồng không tồn tại.");
        if (!FarmRecordRules.ResourceKinds.ContainsKey(f.Kind ?? "")) ModelState.AddModelError(nameof(f.Kind), "Chọn đất hoặc nguồn nước.");
        if (!FarmRecordRules.Statuses.ContainsKey(f.Status ?? "")) ModelState.AddModelError(nameof(f.Status), "Trạng thái không hợp lệ.");
        if (!create && await db.CropJournalEntries.AnyAsync(e => e.WaterResourceId == r.Id) && (r.AreaId != f.AreaId || r.Kind != f.Kind)) ModelState.AddModelError("", "Nguồn đã có nhật ký, không đổi vùng hoặc loại hồ sơ để giữ truy xuất.");
        if (f.Status == "Released" && !await FarmRecordRules.TestsAllowAsync(db, "FieldResource", r.Id)) ModelState.AddModelError(nameof(f.Status), "Còn chỉ tiêu chưa đạt/chưa có kết luận.");
        if (!ModelState.IsValid) { await Areas(); return View("ResourceForm", f); }
        r.AreaId = f.AreaId; r.Kind = f.Kind; r.Name = f.Name; r.Location = f.Location; r.Status = f.Status; r.RiskAssessment = f.RiskAssessment; r.Treatment = f.Treatment;
        if (create) db.FieldResources.Add(r); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu hồ sơ đất / nguồn nước."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> CreateInspection(string? targetType, int? targetId) { await Targets(); return View("InspectionForm", new InspectionForm { TargetType = targetType, TargetId = targetId, SampleDate = DateTime.Today, ReportDate = DateTime.Today }); }
    public async Task<IActionResult> EditInspection(int id)
    { var r = await db.InspectionReports.FindAsync(id); if (r == null) return NotFound(); await Targets(); return View("InspectionForm", new InspectionForm { Id = r.Id, TargetType = r.TargetType, TargetId = r.TargetId, ReportNumber = r.ReportNumber, Laboratory = r.Laboratory, SampleDate = r.SampleDate, ReportDate = r.ReportDate, Indicator = r.Indicator, Result = r.Result, LimitReference = r.LimitReference, Conclusion = r.Conclusion, ExistingEvidence = r.EvidencePath }); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(11 * 1024 * 1024)] public Task<IActionResult> CreateInspection(InspectionForm f) { f.Id = 0; return SaveInspection(f, true); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(11 * 1024 * 1024)] public Task<IActionResult> EditInspection(int id, InspectionForm f) => id == f.Id ? SaveInspection(f, false) : Task.FromResult<IActionResult>(NotFound());
    private async Task<IActionResult> SaveInspection(InspectionForm f, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var r = create ? new InspectionReport() : await db.InspectionReports.FindAsync(f.Id); if (r == null) return NotFound();
        if (!await FarmRecordRules.TargetExistsAsync(db, f.TargetType, f.TargetId)) ModelState.AddModelError(nameof(f.TargetId), "Hồ sơ lấy mẫu không tồn tại hoặc đã lưu trữ.");
        if (!create && (r.TargetType != f.TargetType || r.TargetId != f.TargetId)) ModelState.AddModelError("", "Không chuyển phiếu kiểm nghiệm sang đối tượng khác; tạo phiếu mới để giữ căn cứ.");
        byte[]? bytes = null; string? extension = null;
        if (f.Evidence != null && f.Evidence.Length > 0)
        {
            if (f.Evidence.Length > 10 * 1024 * 1024) ModelState.AddModelError(nameof(f.Evidence), "Tệp tối đa 10 MB.");
            else { using var memory = new MemoryStream(); await f.Evidence.CopyToAsync(memory); bytes = memory.ToArray(); extension = bytes.Length >= 5 && System.Text.Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-" ? ".pdf" : bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png" : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg" : null; if (extension == null) ModelState.AddModelError(nameof(f.Evidence), "Chọn PDF, JPG hoặc PNG thực sự; không đổi đuôi tệp để tải lên."); }
        }
        if (!ModelState.IsValid) { f.ExistingEvidence = r.EvidencePath; await Targets(); return View("InspectionForm", f); }
        string? path = null;
        try
        {
            if (bytes != null && extension != null) { path = "/uploads/evidence/" + Guid.NewGuid().ToString("N") + extension; var folder = Path.Combine(env.WebRootPath, "uploads", "evidence"); Directory.CreateDirectory(folder); await System.IO.File.WriteAllBytesAsync(Path.Combine(folder, Path.GetFileName(path)), bytes); }
            r.TargetType = f.TargetType; r.TargetId = f.TargetId; r.ReportNumber = f.ReportNumber; r.Laboratory = f.Laboratory; r.SampleDate = f.SampleDate?.Date; r.ReportDate = f.ReportDate?.Date; r.Indicator = f.Indicator?.Trim(); r.Result = f.Result; r.LimitReference = f.LimitReference; r.Conclusion = f.Conclusion; r.EvidencePath = path ?? r.EvidencePath;
            if (create) db.InspectionReports.Add(r);
            if (r.Conclusion != "Pass")
            { if (r.TargetType == "InputLot") { var lot = await db.InputLots.FindAsync(r.TargetId); if (lot != null) lot.Status = "Hold"; } if (r.TargetType == "FieldResource") { var source = await db.FieldResources.FindAsync(r.TargetId); if (source != null) source.Status = "Hold"; } if (r.TargetType == "HarvestTrace") { var trace = await db.HarvestTraces.FindAsync(r.TargetId); if (trace != null) trace.IsVoided = true; } }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch { if (path != null) System.IO.File.Delete(Path.Combine(env.WebRootPath, "uploads", "evidence", Path.GetFileName(path))); throw; }
        TempData["Success"] = "Đã lưu phiếu. Kết quả chưa đạt/chưa kết luận sẽ tạm giữ vật tư hoặc nguồn liên quan; kết quả đạt không tự động cho sử dụng."; return RedirectToAction(nameof(Index));
    }
    private async Task Areas() => ViewBag.Areas = await db.Areas.OrderBy(a => a.Name).ToListAsync();
    private async Task Targets() { ViewBag.Inputs = await db.InputLots.ToListAsync(); ViewBag.Resources = await db.FieldResources.ToListAsync(); ViewBag.Harvests = await db.HarvestTraces.ToListAsync(); }
}
