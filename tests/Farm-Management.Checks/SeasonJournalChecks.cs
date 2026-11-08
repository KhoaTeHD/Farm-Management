using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class SeasonJournalChecks
{
    public static void Unit(Action<bool, string> check)
    {
        CropJournalForm Form() => new() { PlantId = 1, RecordCode = Guid.NewGuid().ToString("N"), Date = new(2024, 1, 1), Kind = "Observation", Recorder = "Bác Ba", Summary = "Cây có 4 lá", GrowthStage = "Seedling", Condition = "Good" };
        bool Valid(object f) => Validator.TryValidateObject(f, new(f), new List<ValidationResult>(), true);
        var f = Form(); check(Valid(f), "Growth observations allow genuinely unmeasured metrics");
        f.SurvivalPercent = 101; check(!Valid(f), "Growth survival percentage must not exceed 100");
        f = Form(); f.HeightCm = -1; check(!Valid(f), "Negative crop height is rejected");
        f = Form(); f.Date = DateTime.Today.AddDays(1); check(!Valid(f), "Future actual journals are rejected");
        f = Form(); f.Kind = "Water"; f.Amount = 20; f.Unit = "kg"; check(!Valid(f), "Water quantities must use volume units");
        f.Unit = "l"; check(Valid(f), "Valid water log accepts litres");
        f.Kind = "Fertilize"; check(!Valid(f), "Fertilizer log needs the actual product name");
        f.Kind = "Pesticide"; f.Material = "Sản phẩm trên nhãn"; check(!Valid(f), "Pesticide withdrawal period cannot be guessed as zero");
        f.WithdrawalDays = 0; check(Valid(f), "Explicitly entered zero-day withdrawal can be recorded");
        f.Amount = .001m; check(!Valid(f), "Journal quantities reject unsupported decimal precision");
        f = Form(); f.Kind = "Harvest"; f.Amount = 20; f.Unit = "kg"; check(!Valid(f), "Harvest requires quality description");
        check(CropSeasonRules.PlanDate(new DateTime(2024, 2, 1), 0) == new DateTime(2024, 2, 1) && CropSeasonRules.PlanDate(new DateTime(2024, 2, 1), 30) == new DateTime(2024, 3, 2), "Plan uses day zero and leap-year calendar correctly");
        check(CropSeasonRules.PlanDate(new DateTime(9998, 12, 31), 36500) == null, "Plans cannot overflow supported dates");
        check(!CropSeasonRules.PhotoIsLocal("https://outside.example/a.png") && !CropSeasonRules.PhotoIsLocal("/uploads/crops/../../x.svg"), "Only application-owned raster photo paths can render");
        var model = new CropSeasonViewModel { Plant = new() { Id = 1, PlantingDate = new(2024, 1, 1) }, Steps = new() { new() { Id = 1, PlantId = 1, Kind = "Water", PlannedDay = 0 } }, Entries = new() { new() { Id = 1, PlantId = 1, CareStepId = 1, Kind = "Water", Date = new(2024, 1, 1), Recorder = "QA", Summary = "Tưới" } } };
        check(model.DoneSteps == 0, "Incomplete soft-data journal cannot mark a plan complete");
    }

    public static async Task Integration(HttpClient client, DbContextOptions<ApplicationDbContext> options, Action<bool, string> check)
    {
        int plantId, secondId, typeId, seedId, areaId, stepId, fertilizerId, pesticideId, waterId;
        await using (var db = new ApplicationDbContext(options))
        {
            var type = new PlantType { Name = "Cà chua theo mùa", GrowthCycleDays = 90 }; db.PlantTypes.Add(type);
            var area = new Area { Name = "Vườn theo dõi mùa vụ", Acreage = 500 }; db.Areas.Add(area); await db.SaveChangesAsync();
            var seed = new Seed { Name = "Giống mùa vụ QA", PlantTypeId = type.Id, Origin = "Nhà vườn QA" }; db.Seeds.Add(seed); await db.SaveChangesAsync();
            var p = new Plant { BatchCode = "SEASON-QA-01", SeasonName = "Vụ cà chua trước", AreaId = area.Id, PlantTypeId = type.Id, SeedId = seed.Id, PlantingDate = new(2024, 1, 1), Quantity = 100, Status = "Growing" };
            var p2 = new Plant { BatchCode = "SEASON-QA-02", SeasonName = "Vụ cà chua sau", AreaId = area.Id, PlantTypeId = type.Id, SeedId = seed.Id, PlantingDate = new(2024, 6, 1), Quantity = 100, Status = "Growing" };
            db.Plants.AddRange(p, p2); await db.SaveChangesAsync(); plantId = p.Id; secondId = p2.Id; typeId = type.Id; seedId = seed.Id; areaId = area.Id;
            var fertilizer = new InputLot { Supplier="Nguồn QA",ManufacturerBatch="NSX-FERT-QA",ExpiryDate=new(2030,1,1),Code="SEASON-FERT",Kind="Fertilizer",ProductName="Tên sản phẩm QA",Quantity=1000,Unit="kg",ReceivedDate=new(2023,1,1),Status="Released" };
            var pesticide = new InputLot { Supplier="Nguồn QA",ManufacturerBatch="NSX-PEST-QA",RegistrationNumber="DK-QA",ExpiryDate=new(2030,1,1),Code="SEASON-PEST",Kind="Pesticide",ProductName="Sản phẩm QA",Quantity=1000,Unit="ml",ReceivedDate=new(2023,1,1),Status="Released" };
            var water = new FieldResource {AreaId=area.Id,Kind="Water",Name="Giếng QA",Location="Phía đông QA",RiskAssessment="Đã kiểm tra nguồn thải",Status="Released"};
            db.InputLots.AddRange(fertilizer,pesticide);db.FieldResources.Add(water);await db.SaveChangesAsync();fertilizerId=fertilizer.Id;pesticideId=pesticide.Id;waterId=water.Id;
            db.WaterIrrigationLogs.Add(new() { PlantId = p.Id, IrrigationDate = new(2024, 1, 2), WaterAmount = 100, Notes = "Dữ liệu cũ phải giữ" }); await db.SaveChangesAsync();
        }
        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        check(home.Contains("Hướng dẫn ghi sổ từng bước") && home.Contains("ngày 0") && home.Contains("để trống, không nhập 0") && home.Contains("tổng lượng cho cả lô"), "Dashboard includes visible detailed farmer instructions and concrete input examples");
        foreach (var path in new[] { $"/Plants/Details/{plantId}", $"/Plants/Evaluation/{plantId}", $"/Plants/Compare?leftId={plantId}&rightId={secondId}", "/Plants/Compare?leftId=999999", "/Plants/Details/1", "/CropJournal/Edit/1", "/CropJournal/Delete/1", "/CarePlans/Edit/1", "/CarePlans/Delete/1" })
            check((await client.GetAsync(path)).StatusCode == HttpStatusCode.OK, "Season pages handle missing soft values: " + path);
        Dictionary<string,string> Plan(string title = "Kiểm tra cây sống") => new() { ["PlantId"] = plantId.ToString(), ["Title"] = title, ["Kind"] = "Observation", ["PlannedDay"] = "7", ["Instructions"] = "Đếm 100 cây đại diện và ghi tình trạng lá" };
        var planRaces = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/CarePlans/Create", Plan(), $"/CarePlans/Create?plantId={plantId}")));
        check(planRaces.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1 && planRaces.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, "Concurrent identical care plans are serialized");
        await using (var db = new ApplicationDbContext(options)) stepId = (await db.CropCareSteps.SingleAsync(s => s.PlantId == plantId)).Id;
        Dictionary<string,string> Entry(string kind="Observation", string date="2024-01-08") => new() { ["InputLotId"] = kind=="Fertilize"?fertilizerId.ToString():pesticideId.ToString(), ["WaterResourceId"]=waterId.ToString(), ["PlantId"] = plantId.ToString(), ["RecordCode"] = Guid.NewGuid().ToString("N"), ["Date"] = date, ["Kind"] = kind, ["Recorder"] = "Bác Ba QA", ["Summary"] = "Cây có 4 lá, góc thấp hơi vàng", ["GrowthStage"] = "Seedling", ["Condition"] = "Watch", ["HeightCm"] = "12", ["SurvivalPercent"] = "95", ["AffectedPercent"] = "5" };
        Task<HttpResponseMessage> Add(Dictionary<string,string> fields) => Post("/CropJournal/Create", fields, $"/CropJournal/Create?plantId={plantId}");
        var observation = Entry(); observation["CareStepId"] = stepId.ToString();
        var same = observation.ToDictionary(x => x.Key, x => x.Value);
        var races = await Task.WhenAll(Add(observation), Add(same));
        check(races.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1, "Double-submitted journal is written once under shared transaction lock");
        int observationId;
        await using (var db = new ApplicationDbContext(options))
        {
            observationId = (await db.CropJournalEntries.SingleAsync(e => e.RecordCode == observation["RecordCode"])).Id;
            var m = await new CropSeasonService(db).LoadAsync(plantId);
            check(m!.DoneSteps == 1 && m.LatestObservation?.HeightCm == 12 && m.LatestObservation.SurvivalPercent == 95, "Growth measurements and actual plan completion appear in the season notebook");
            check(m.Timeline.Any(t => t.Title.Contains("sổ cũ") && t.Description!.Contains("Dữ liệu cũ phải giữ")), "Existing irrigation logs remain in the unified timeline");
        }
        var wrong = Entry(); wrong["PlantId"] = secondId.ToString(); wrong["CareStepId"] = stepId.ToString();
        check((await Add(wrong)).StatusCode == HttpStatusCode.OK, "Plan references cannot cross seasons");
        wrong = Entry(date:"2023-12-31"); check((await Add(wrong)).StatusCode == HttpStatusCode.OK, "Journal cannot predate planting");
        wrong = Entry(); wrong["PlantId"] = "999999"; check((await Add(wrong)).StatusCode == HttpStatusCode.OK, "Journal rejects a missing lot");
        wrong = Entry(); wrong["SurvivalPercent"] = "101"; check((await Add(wrong)).StatusCode == HttpStatusCode.OK, "MVC enforces growth percentage validation");
        check((await client.PostAsync("/CropJournal/Create", new FormUrlEncodedContent(Entry()))).StatusCode == HttpStatusCode.BadRequest, "Journal writes require antiforgery tokens");
        var editPlan = Plan("Đổi kế hoạch sau khi làm"); editPlan["Id"] = stepId.ToString();
        check((await Post($"/CarePlans/Edit/{stepId}", editPlan)).StatusCode == HttpStatusCode.OK, "Plan with actual journals cannot be rewritten to manufacture adherence");
        await Post($"/CarePlans/Delete/{stepId}", new() { ["id"] = stepId.ToString() });
        await using (var db = new ApplicationDbContext(options)) check(await db.CropCareSteps.AnyAsync(s => s.Id == stepId), "Used care plan cannot be deleted through a forged request");
        await using (var db = new ApplicationDbContext(options))
        {
            var broken = await db.CropCareSteps.FindAsync(stepId); broken!.PlannedDay = null; await db.SaveChangesAsync();
            var notebook = await new CropSeasonService(db).LoadAsync(plantId);
            check(notebook!.IncompleteSteps == 1 && notebook.DoneSteps == 0,"Incomplete soft plan cannot appear as a valid completed procedure");
        }
        var repairPlan=Plan();repairPlan["Id"]=stepId.ToString();
        check((await Post($"/CarePlans/Edit/{stepId}",repairPlan)).StatusCode==HttpStatusCode.Redirect,"Incomplete legacy plan can be repaired without rewriting valid prior plans");
        foreach (var kind in new[] { "Water", "Fertilize", "Care" })
        {
            var e = Entry(kind); e["Amount"] = "20"; e["Unit"] = kind=="Water"?"l":"kg"; e["Material"] = "Tên sản phẩm QA";
            check((await Add(e)).StatusCode == HttpStatusCode.Redirect, "Record actual care: " + kind);
        }
        var spray = Entry("Pesticide", "2024-02-01"); spray["Amount"] = "30"; spray["Unit"] = "ml"; spray["Material"] = "Sản phẩm QA"; spray["WithdrawalDays"] = "7";
        check((await Add(spray)).StatusCode == HttpStatusCode.Redirect, "Pesticide journal persists its explicit withdrawal interval");
        var harvest = Entry("Harvest", "2024-02-07"); harvest["Amount"] = "35"; harvest["Unit"] = "kg"; harvest["Quality"] = "Quả đều";
        check((await Add(harvest)).StatusCode == HttpStatusCode.OK, "Harvest before withdrawal deadline is blocked");
        harvest["Date"] = "2024-02-08";
        check((await Add(harvest)).StatusCode == HttpStatusCode.Redirect, "Harvest on withdrawal deadline can be recorded");
        var backdated = Entry("Pesticide", "2024-02-05"); backdated["Amount"] = "10"; backdated["Unit"] = "ml"; backdated["Material"] = "Sản phẩm QA"; backdated["WithdrawalDays"] = "10";
        check((await Add(backdated)).StatusCode == HttpStatusCode.OK, "Backdated pesticide cannot invalidate a recorded later harvest");
        var later = Entry("Observation", "2024-02-10"); later.Remove("SurvivalPercent"); later["HeightCm"] = "35"; later["GrowthStage"] = "Flowering";
        check((await Add(later)).StatusCode == HttpStatusCode.Redirect, "Later growth observation may leave unmeasured survival unknown");
        await using (var db = new ApplicationDbContext(options))
        {
            var m = await new CropSeasonService(db).LoadAsync(plantId);
            check(m!.HarvestKg == 35 && m.LatestObservation?.HeightCm == 35 && m.LatestObservation.SurvivalPercent == null, "Latest growth does not borrow an older survival estimate; harvest aggregates recorded kg");
        }
        await Post($"/Plants/Evaluation/{plantId}", new() { ["Id"] = plantId.ToString(), ["CareProtocol"] = "Theo dõi đất trước tưới", ["SeasonReview"] = "Góc trũng vàng lá sau mưa", ["NextSeasonChanges"] = "Làm rãnh thoát nước trước vụ sau" });
        var comparison = WebUtility.HtmlDecode(await client.GetStringAsync($"/Plants/Compare?leftId={plantId}&rightId={secondId}"));
        check(comparison.Contains("Làm rãnh thoát nước") && comparison.Contains("chưa quy đổi theo diện tích"), "Season comparison shows farmer lessons without claiming causal superiority");
        var clone = new Dictionary<string,string> { ["BatchCode"] = "SEASON-CLONE", ["SeasonName"] = "Vụ mới có điều chỉnh", ["AreaId"] = areaId.ToString(), ["PlantTypeId"] = typeId.ToString(), ["SeedId"] = seedId.ToString(), ["PlantingDate"] = "2025-01-01", ["Status"] = "Growing", ["SourcePlantId"] = plantId.ToString() };
        check((await Post("/Plants/Create", clone)).StatusCode == HttpStatusCode.Redirect, "New season can reuse a previous compatible plan");
        await using (var db = new ApplicationDbContext(options))
        {
            var copy = await db.Plants.SingleAsync(p => p.BatchCode == "SEASON-CLONE");
            check(await db.CropCareSteps.CountAsync(s => s.PlantId == copy.Id) == 1 && !await db.CropJournalEntries.AnyAsync(e => e.PlantId == copy.Id), "Plan reuse copies intentions but never old growth, photos or harvest outcomes");
        }
        await Post($"/Plants/Delete/{plantId}", new() { ["id"] = plantId.ToString() });
        await using (var db = new ApplicationDbContext(options)) check(await db.Plants.AnyAsync(p => p.Id == plantId), "Season with journals is protected against deletion in the backend");
        var changedDate = clone.ToDictionary(x=>x.Key,x=>x.Value); changedDate["Id"]=plantId.ToString(); changedDate["BatchCode"]="SEASON-QA-01"; changedDate["PlantingDate"]="2024-03-01";
        check((await Post($"/Plants/Edit/{plantId}", changedDate)).StatusCode == HttpStatusCode.OK, "Planting date cannot be moved past existing care journals");
        await PhotoChecks();
        var changedObservation = observation.ToDictionary(x=>x.Key,x=>x.Value); changedObservation.Remove("OriginalRevision"); changedObservation["Id"]=observationId.ToString(); changedObservation["HeightCm"]="13";
        check((await Post($"/CropJournal/Edit/{observationId}", changedObservation)).StatusCode==HttpStatusCode.Redirect,"A mistaken growth measurement can be corrected");
        changedObservation["PlantId"]=secondId.ToString();
        check((await Post($"/CropJournal/Edit/{observationId}", changedObservation)).StatusCode==HttpStatusCode.OK,"Existing journal cannot be moved to another valid season");
        await Post($"/CropJournal/Delete/{observationId}",new(){["id"]=observationId.ToString()});
        await using(var db = new ApplicationDbContext(options)) check((await new CropSeasonService(db).LoadAsync(plantId))!.DoneSteps==0,"Deleting actual evidence returns its care plan to unrecorded");
        var restore = Entry();restore["CareStepId"]=stepId.ToString();await Add(restore);
        var simultaneousSpray=Entry("Pesticide","2024-06-03");simultaneousSpray["PlantId"]=secondId.ToString();simultaneousSpray["Material"]="QA";simultaneousSpray["Amount"]="10";simultaneousSpray["Unit"]="ml";simultaneousSpray["WithdrawalDays"]="7";
        var simultaneousHarvest=Entry("Harvest","2024-06-04");simultaneousHarvest["PlantId"]=secondId.ToString();simultaneousHarvest["Amount"]="10";simultaneousHarvest["Unit"]="kg";simultaneousHarvest["Quality"]="QA";
        var concurrentSafety=await Task.WhenAll(Add(simultaneousSpray),Add(simultaneousHarvest));
        check(concurrentSafety.Count(r=>r.StatusCode==HttpStatusCode.Redirect)==1 && concurrentSafety.Count(r=>r.StatusCode==HttpStatusCode.OK)==1,"Concurrent pesticide and conflicting harvest writes cannot both succeed");
        check((await Post("/CarePlans/Create",new(){["PlantId"]=plantId.ToString()},$"/CarePlans/Create?plantId={plantId}")).StatusCode==HttpStatusCode.OK,"Incomplete plan form returns validation rather than throwing on NULL title");
        // Keep realistic observations for preview; verify deletion and plan recalculation on a dedicated extra receipt.
        var remove = Entry("Care"); await Add(remove);
        int removeId;
        await using (var db = new ApplicationDbContext(options)) removeId = (await db.CropJournalEntries.SingleAsync(e => e.RecordCode == remove["RecordCode"])).Id;
        await Post($"/CropJournal/Delete/{removeId}", new() { ["id"] = removeId.ToString() });
        await using (var db = new ApplicationDbContext(options)) check(!await db.CropJournalEntries.AnyAsync(e => e.Id == removeId), "Mistaken actual journal can be deleted through confirmation flow");

        async Task PhotoChecks()
        {
            var e = Entry("Care");
            var html = await client.GetStringAsync($"/CropJournal/Create?plantId={plantId}"); e["__RequestVerificationToken"] = Csrf(html); TestChangeFields.Apply(e,html);
            using var invalid = new MultipartFormDataContent(); foreach (var pair in e) invalid.Add(new StringContent(pair.Value),pair.Key);
            invalid.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>")), "Photo", "fake.jpg");
            check((await client.PostAsync("/CropJournal/Create",invalid)).StatusCode == HttpStatusCode.OK, "Upload rejects executable content disguised as a photo");
            using var valid = new MultipartFormDataContent(); foreach (var pair in e) valid.Add(new StringContent(pair.Value),pair.Key);
            valid.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRz0AAAAASUVORK5CYII=")),"Photo","photo.png");
            check((await client.PostAsync("/CropJournal/Create",valid)).StatusCode == HttpStatusCode.Redirect, "Upload accepts a PNG photo and stores an application-generated filename");
            await using var db = new ApplicationDbContext(options);
            var saved = await db.CropJournalEntries.SingleAsync(j => j.RecordCode==e["RecordCode"]);
            check(CropSeasonRules.PhotoIsLocal(saved.PhotoPath) && (await client.GetAsync(saved.PhotoPath)).StatusCode==HttpStatusCode.OK,"Uploaded crop photo is accessible with a safe local path");
            // This uniquely generated fixture file belongs only to this disposable test run.
            var root = new DirectoryInfo(AppContext.BaseDirectory); while(root!=null && !File.Exists(Path.Combine(root.FullName,"Farm-Management.sln"))) root=root.Parent;
            var path = Path.Combine(root!.FullName,"Farm-Management","wwwroot","uploads","crops",Path.GetFileName(saved.PhotoPath!));
            File.Delete(path); saved.PhotoPath=null; await db.SaveChangesAsync();
        }
        async Task<HttpResponseMessage> Post(string route, Dictionary<string,string> fields,string? tokenRoute=null)
        {
            var html = await client.GetStringAsync(tokenRoute??route); fields["__RequestVerificationToken"]=Csrf(html);
            TestChangeFields.Apply(fields,html);
            return await client.PostAsync(route,new FormUrlEncodedContent(fields));
        }
        static string Csrf(string html)
        {
            var token=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            return token.Length>0?token:throw new Exception("Missing season antiforgery token");
        }
    }
}
