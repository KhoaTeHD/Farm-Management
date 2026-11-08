using Farm_Management.Data;
using Farm_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

// Records the name supplied by the operator; this application does not authenticate that identity.
public class RecordChangeFilter(ApplicationDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        var type = controller switch
        {
            "Areas" => "Area",
            "PlantTypes" => "PlantType",
            "Seeds" => "Seed",
            "Plants" => "Plant",
            "CropJournal" => "CropJournalEntry",
            "CarePlans" => "CropCareStep",
            "HarvestTrace" => "HarvestTrace",
            "FarmRecords" => action.Contains("Input") ? "InputLot" : action.Contains("Resource") ? "FieldResource" : action.Contains("Inspection") ? "InspectionReport" : null,
            "RecordHistory" => context.HttpContext.Request.HasFormContentType ? context.HttpContext.Request.Form["recordType"].ToString() : null,
            _ => null
        };
        if (type == null) { await next(); return; }
        var isPost = HttpMethods.IsPost(context.HttpContext.Request.Method);
        var creating = action.StartsWith("Create", StringComparison.Ordinal);
        if (isPost)
        {
            var fields = await context.HttpContext.Request.ReadFormAsync();
            var actor = fields["ChangedBy"].ToString().Trim(); if (actor.Length == 0) actor = fields["Recorder"].ToString().Trim();
            var reason = fields["ChangeReason"].ToString().Trim();
            if (actor.Length is < 1 or > 100 || reason.Length > 1000 || (!creating && reason.Length == 0))
            { context.Result = new BadRequestObjectResult("Điền tên người ghi và lý do sửa / lưu trữ / khôi phục (tối đa 1.000 ký tự), rồi gửi lại."); return; }
            db.ChangeActor = actor; db.ChangeReason = reason;
            if (!creating)
            {
                if (!int.TryParse(fields["OriginalRevision"], out var version) || version < 0)
                { context.Result = new BadRequestObjectResult("Thiếu phiên bản hồ sơ. Mở lại trang từ danh sách để lấy phiên bản hiện tại."); return; }
                var idText = context.RouteData.Values["id"]?.ToString() ?? fields["id"].ToString();
                if (!int.TryParse(idText, out var id)) { context.Result = new BadRequestResult(); return; }
                db.ExpectedRecordType = type; db.ExpectedRecordId = id; db.ExpectedRevision = version;
            }
        }
        var result = await next();
        if (result.Exception is DbUpdateConcurrencyException)
        { result.ExceptionHandled = true; result.Result = new ConflictObjectResult("Hồ sơ đã được người khác sửa. Tải lại trang, kiểm tra nội dung mới và nhập lại thay đổi để không ghi đè dữ liệu."); return; }
        if (result.Result is ViewResult view)
        {
            var id = (view.Model?.GetType().GetProperty("Id")?.GetValue(view.Model) as int?) ?? 0;
            var entity = await FindAsync(db, type, id);
            view.ViewData!["RecordRevision"] = isPost && !creating ? db.ExpectedRevision ?? 0 : entity?.Revision ?? 0;
            view.ViewData["ChangeIsEdit"] = !creating;
        }
    }
    public static async Task<ICultivationRecord?> FindAsync(ApplicationDbContext db, string type, int id) => type switch
    {
        "Area" => await db.Areas.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "PlantType" => await db.PlantTypes.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "Seed" => await db.Seeds.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "Plant" => await db.Plants.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "CropJournalEntry" => await db.CropJournalEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "CropCareStep" => await db.CropCareSteps.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "InputLot" => await db.InputLots.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "FieldResource" => await db.FieldResources.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "InspectionReport" => await db.InspectionReports.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        "HarvestTrace" => await db.HarvestTraces.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        _ => null
    };
}
