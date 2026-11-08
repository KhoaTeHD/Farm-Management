using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class MaterialUsageChecks
{
    public static void Unit(Action<bool,string> check)
    {
        var rows=MaterialUsageService.FromJournal(new[]{
            new CropJournalEntry {Id=1,Kind="Fertilize",Material="NPK đã dùng",Amount=1.25m,Unit="kg",Recorder="Người bón",Date=new(2024,1,4)},
            new CropJournalEntry {Id=2,Kind="Pesticide",Material="Thuốc đã dùng",Amount=250,Unit="ml",Recorder="Người phun",Date=new(2024,1,5),WithdrawalDays=7},
            new CropJournalEntry {Id=3,Kind="Water"},new CropJournalEntry {Id=4,Kind="Pesticide",Material="Đã lưu trữ",IsDeleted=true},
            new CropJournalEntry {Id=5,Kind="Pesticide"}
        });
        check(rows.Count==3 && rows[0].ProductName=="NPK đã dùng" && rows[0].QuantityText=="1,25 kg" && rows[1].PerformedBy=="Người phun","Usage table uses recorded product, quantity/unit and performer for each application");
        check(rows[2].ProductName==null && rows[2].QuantityText==null && rows[2].WithdrawalDays==null,"Missing usage data remains unknown rather than borrowing catalogue values or zero days");
        check(MaterialUsageService.WithdrawalEnd(rows[1])==new DateTime(2024,1,12),"Withdrawal end is derived from actual application date and recorded days");
        check(MaterialUsageService.WithdrawalEnd(new(){Date=new(2024,1,5),WithdrawalDays=0})==new DateTime(2024,1,5),"Explicit zero withdrawal days remains a recorded zero");
        check(MaterialUsageService.WithdrawalEnd(new(){Date=DateTime.MaxValue,WithdrawalDays=7})==null && MaterialUsageService.WithdrawalEnd(new(){WithdrawalDays=7})==null,"Unknown dates and date overflow cannot fabricate withdrawal deadlines");
    }
    public static async Task Integration(HttpClient client,DbContextOptions<ApplicationDbContext> options,Action<bool,string> check)
    {
        int plantId,traceId,oldTraceId,registryId,workerId,journalId;
        await using(var db=new ApplicationDbContext(options))
        {
            var area=new Area{Name="Vùng bảng sử dụng QA"};var type=new PlantType{Name="Cây bảng sử dụng QA"};db.AddRange(area,type);await db.SaveChangesAsync();
            var seed=new Seed{Name="Giống bảng sử dụng QA",PlantTypeId=type.Id};db.Add(seed);await db.SaveChangesAsync();
            var plant=new Plant{BatchCode="MATERIAL-TABLE-QA",SeasonName="Vụ kiểm thử bảng thuốc phân",AreaId=area.Id,PlantTypeId=type.Id,SeedId=seed.Id,PlantingDate=new(2024,1,1)};
            var worker=new Worker{Code="MATERIAL-WORKER-QA",FullName="Người thực hiện sổ cũ"};
            var fertilizer=new Fertilizer{Name="Phân sổ cũ đã đối chiếu"};var registry=new PesticideRegistry{TradeName="Thuốc sổ cũ đã đối chiếu"};
            db.AddRange(plant,worker,fertilizer,registry);await db.SaveChangesAsync();plantId=plant.Id;workerId=worker.Id;registryId=registry.Id;
            var pesticide=new Pesticide{PesticideRegistryId=registry.Id};db.Add(pesticide);await db.SaveChangesAsync();
            db.FertilizationLogs.Add(new(){PlantId=plant.Id,FertilizerId=fertilizer.Id,WorkerId=worker.Id,ApplicationDate=new(2024,1,2),Dosage="2 kg cho cả lô"});
            db.PesticideApplicationLogs.Add(new(){PlantId=plant.Id,PesticideId=pesticide.Id,WorkerId=worker.Id,ApplicationDate=new(2024,1,3),Dosage="20 ml",PHI_Days=7});
            db.FertilizationLogs.Add(new(){PlantId=plant.Id,FertilizerId=999999,WorkerId=999999});
            var recorded=new CropJournalEntry{PlantId=plant.Id,Kind="Pesticide",Material="Thuốc được ghi lúc phun",Date=new(2024,1,5),Amount=250,Unit="ml",Recorder="Bác phun thực tế",WithdrawalDays=7,Summary="Lần phun thực tế"};
            var harvest=new CropJournalEntry{PlantId=plant.Id,Kind="Harvest",Date=new(2024,1,20),Amount=10,Unit="kg",Quality="QA",Recorder="QA",Summary="Thu hoạch QA"};
            db.CropJournalEntries.AddRange(recorded,harvest,
                new(){PlantId=plant.Id,Kind="Fertilize",Material="Phân được ghi lúc bón",Date=new(2024,1,4),Amount=1.25m,Unit="kg",Recorder="Bác bón thực tế",Summary="Lần bón thực tế"},
                new(){PlantId=plant.Id,Kind="Pesticide",Material="Thuốc thiếu dữ liệu"},
                new(){PlantId=plant.Id,Kind="Pesticide",Material="Thuốc ghi không cách ly",Date=new(2024,1,6),WithdrawalDays=0},
                new(){PlantId=plant.Id,Kind="Fertilize",Material="Phân đã chuyển lưu trữ",IsDeleted=true},
                new(){PlantId=plant.Id,Kind="Pesticide",Material="Thuốc dùng sau đợt thu hoạch",Date=new(2024,1,25),WithdrawalDays=3});
            await db.SaveChangesAsync();journalId=recorded.Id;
            var service=new HarvestTraceService(db);var snapshot=await service.BuildAsync(plantId,harvest.Date,harvest.Amount,harvest.Quality);
            check(snapshot!.LegacyMaterialUses?.Any(r=>r.ProductName=="Thuốc sổ cũ đã đối chiếu" && r.PerformedBy==worker.FullName)==true,"Trace captures named legacy products and performers into its saved snapshot");
            var trace=new HarvestTrace{PlantId=plantId,JournalEntryId=harvest.Id,IsVoided=false};await service.FillAsync(trace,snapshot);db.Add(trace);await db.SaveChangesAsync();traceId=trace.Id;
            var oldJson=JsonNode.Parse(trace.SnapshotJson!)!;oldJson.AsObject().Remove("LegacyMaterialUses");
            var older=new HarvestTrace{PlantId=plantId,Code="OLD-MATERIAL-TABLE",HarvestDate=trace.HarvestDate,WeightKg=trace.WeightKg,Quality=trace.Quality,IsVoided=false,SnapshotJson=oldJson.ToJsonString()};older.SourceHash=HarvestTraceService.Hash(older.SnapshotJson);db.Add(older);await db.SaveChangesAsync();oldTraceId=older.Id;
            check(HarvestTraceService.Read(older)!=null,"Older trace JSON without the new display detail remains readable");
        }
        var season=Table(await client.GetStringAsync($"/Plants/Details/{plantId}"));
        check(Row(season,"Thuốc được ghi lúc phun").Contains("250 ml") && Row(season,"Thuốc được ghi lúc phun").Contains("Bác phun thực tế") && Row(season,"Thuốc được ghi lúc phun").Contains("7 ngày") && Row(season,"Thuốc được ghi lúc phun").Contains("05/01/2024"),"Season shows the product, use date, quantity, performer and withdrawal in the same application row");
        check(Row(season,"Phân được ghi lúc bón").Contains("1,25 kg") && Row(season,"Phân được ghi lúc bón").Contains("Bác bón thực tế") && Row(season,"Phân được ghi lúc bón").Contains("Chưa ghi nhận"),"Fertilizer application is visible and does not assume an unrecorded withdrawal period");
        check(season.Contains("Phân sổ cũ đã đối chiếu") && season.Contains("Thuốc sổ cũ đã đối chiếu") && season.Contains("Người thực hiện sổ cũ") && season.Contains("2 kg cho cả lô"),"Season table includes readable fertilizer and pesticide applications from legacy logs");
        check(season.Contains("Chưa xác định sản phẩm") && season.Contains("Chưa ghi người thực hiện") && season.Contains("Chưa ghi lượng dùng") && !season.Contains("Phân đã chuyển lưu trữ"),"Orphan and NULL usage rows are explicit while archived applications stay hidden");
        check(Row(season,"Thuốc ghi không cách ly").Contains("0 ngày"),"Rendered zero-day withdrawal is distinguished from unknown withdrawal");
        var traceTable=Table(await client.GetStringAsync($"/HarvestTrace/Details/{traceId}"));
        check(traceTable.Contains("Thuốc được ghi lúc phun") && traceTable.Contains("Phân được ghi lúc bón") && traceTable.Contains("Thuốc sổ cũ đã đối chiếu") && !traceTable.Contains("Thuốc dùng sau đợt thu hoạch"),"Harvest trace shows saved applications up to its harvest date, including legacy detail");
        await using(var db=new ApplicationDbContext(options))
        {
            (await db.PesticideRegistries.FindAsync(registryId))!.TradeName="Tên danh mục thay đổi sau thu hoạch";
            (await db.Workers.FindAsync(workerId))!.FullName="Tên công nhân thay đổi sau thu hoạch";
            var e=await db.CropJournalEntries.FindAsync(journalId);e!.Material="Tên thuốc trong nhật ký đã sửa";e.Recorder="Người thực hiện đã sửa";e.WithdrawalDays=14;await db.SaveChangesAsync();
        }
        traceTable=Table(await client.GetStringAsync($"/HarvestTrace/Details/{traceId}"));
        check(traceTable.Contains("Thuốc được ghi lúc phun") && traceTable.Contains("Bác phun thực tế") && traceTable.Contains("Thuốc sổ cũ đã đối chiếu") && traceTable.Contains("Người thực hiện sổ cũ") && !traceTable.Contains("thay đổi sau thu hoạch") && !traceTable.Contains("Người thực hiện đã sửa"),"Trace display preserves original saved names, performers and withdrawal after live data changes");
        var oldTable=Table(await client.GetStringAsync($"/HarvestTrace/Details/{oldTraceId}"));
        check(oldTable.Contains("Bản truy xuất cũ chưa lưu bảng chi tiết") && !oldTable.Contains("Tên danh mục thay đổi") && !oldTable.Contains("Người thực hiện sổ cũ"),"Older snapshots disclose missing legacy detail without silently rebuilding history from current catalogues");
    }
    private static string Table(string html)
    {
        var found=Regex.Match(html,"id=\"material-usage\"[^>]*>([\\s\\S]*?)</section>");
        if(!found.Success)throw new Exception("Missing material usage section");
        return WebUtility.HtmlDecode(found.Groups[1].Value);
    }
    private static string Row(string table,string name)=>Regex.Matches(table,"<tr data-list-row[\\s\\S]*?</tr>").Select(m=>m.Value).First(r=>r.Contains(name));
}
