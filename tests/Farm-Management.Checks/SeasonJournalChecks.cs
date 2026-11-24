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
        CropJournalForm Form() => new()
        {
            PlantId = 1,
            RecordCode = Guid.NewGuid().ToString("N"),
            Date = new(2024, 1, 1),
            Kind = "Observation",
            WorkerIds = new() { 1 }
        };
        bool Valid(object f) => Validator.TryValidateObject(f, new(f), new List<ValidationResult>(), true);

        var f = Form();
        check(Valid(f), "Minimal daily work form allows optional summary, amount, unit, and observation metrics");

        f.WorkerIds = new();
        check(!Valid(f), "Daily work requires at least one worker");

        f = Form();
        f.SurvivalPercent = 101;
        check(!Valid(f), "Growth survival percentage must not exceed 100");

        f = Form();
        f.HeightCm = -1;
        check(!Valid(f), "Negative crop height is rejected");

        f = Form();
        f.Date = DateTime.Today.AddDays(1);
        check(!Valid(f), "Future actual journals are rejected");

        f = Form();
        f.Kind = "Fertilize";
        f.Amount = .001m;
        check(!Valid(f), "Journal quantities reject unsupported decimal precision");

        f = Form();
        f.Kind = "Pesticide";
        f.WithdrawalDays = 0;
        check(Valid(f), "Explicitly entered zero-day withdrawal can be recorded");

        f = Form();
        f.Kind = "Harvest";
        check(Valid(f), "Simplified harvest allows optional amount and quality");

        var mat = new MaterialItemForm { Name = "Phân gà", Category = "Fertilizer", DefaultUnit = "bao" };
        check(Valid(mat), "MaterialItemForm accepts simple catalog input");
        mat.Category = "Invalid";
        check(!Valid(mat), "MaterialItemForm rejects unknown category");

        check(CropSeasonRules.PlanDate(new DateTime(2024, 2, 1), 0) == new DateTime(2024, 2, 1)
            && CropSeasonRules.PlanDate(new DateTime(2024, 2, 1), 30) == new DateTime(2024, 3, 2),
            "Plan uses day zero and leap-year calendar correctly");
        check(CropSeasonRules.PlanDate(new DateTime(9998, 12, 31), 36500) == null,
            "Plans cannot overflow supported dates");
        check(!CropSeasonRules.PhotoIsLocal("https://outside.example/a.png") && !CropSeasonRules.PhotoIsLocal("/uploads/crops/../../x.svg"),
            "Only application-owned raster photo paths can render");

        var model = new CropSeasonViewModel
        {
            Plant = new() { Id = 1, PlantingDate = new(2024, 1, 1) },
            Steps = new() { new() { Id = 1, PlantId = 1, Kind = "Water", PlannedDay = 0 } },
            Entries = new() { new() { Id = 1, PlantId = 1, CareStepId = 1, Kind = "Water", Date = new(2024, 1, 1) } }
        };
        check(model.DoneSteps == 0, "Journal entry without assigned workers or recorder cannot mark a plan complete");
    }

    public static async Task Integration(HttpClient client, DbContextOptions<ApplicationDbContext> options, Action<bool, string> check)
    {
        int plantId, secondId, typeId, seedId, areaId, stepId;
        int w1Id, w2Id, wInactiveId;
        int chickenManureId, cowManureId, jpPesticideId, unusedMatId;

        await using (var db = new ApplicationDbContext(options))
        {
            var type = new PlantType { Name = "Cà chua theo mùa", GrowthCycleDays = 90 };
            db.PlantTypes.Add(type);
            var area = new Area { Name = "Vườn theo dõi mùa vụ", Acreage = 500 };
            db.Areas.Add(area);
            var w1 = new Worker { Code = "W-DAILY-1", FullName = "Nguyễn Văn A", DailySalary = 250000, IsActive = true };
            var w2 = new Worker { Code = "W-DAILY-2", FullName = "Trần Thị B", DailySalary = 250000, IsActive = true };
            var wInactive = new Worker { Code = "W-DAILY-OFF", FullName = "Lê Văn Nghỉ", DailySalary = 200000, IsActive = false };
            db.Workers.AddRange(w1, w2, wInactive);
            await db.SaveChangesAsync();

            w1Id = w1.Id;
            w2Id = w2.Id;
            wInactiveId = wInactive.Id;

            var seed = new Seed { Name = "Giống mùa vụ QA", PlantTypeId = type.Id, Origin = "Nhà vườn QA" };
            db.Seeds.Add(seed);
            await db.SaveChangesAsync();

            var p = new Plant { BatchCode = "SEASON-QA-01", SeasonName = "Vụ cà chua trước", AreaId = area.Id, PlantTypeId = type.Id, SeedId = seed.Id, PlantingDate = new(2024, 1, 1), Quantity = 100, Status = "Growing" };
            var p2 = new Plant { BatchCode = "SEASON-QA-02", SeasonName = "Vụ cà chua sau", AreaId = area.Id, PlantTypeId = type.Id, SeedId = seed.Id, PlantingDate = new(2024, 6, 1), Quantity = 100, Status = "Growing" };
            db.Plants.AddRange(p, p2);
            await db.SaveChangesAsync();

            plantId = p.Id;
            secondId = p2.Id;
            typeId = type.Id;
            seedId = seed.Id;
            areaId = area.Id;

            db.WaterIrrigationLogs.Add(new() { PlantId = p.Id, IrrigationDate = new(2024, 1, 2), WaterAmount = 100, Notes = "Dữ liệu cũ phải giữ" });
            await db.SaveChangesAsync();
        }

        // Dashboard reflects simplified workflow and quick daily work actions
        var home = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        check(home.Contains("4 bước quản lý nông trại gọn nhẹ") && home.Contains("Ghi việc hôm nay") && home.Contains("Danh mục vật liệu"),
            "Dashboard includes simplified 4-step guide and quick daily work links");

        foreach (var path in new[]
        {
            $"/Plants/Details/{plantId}", $"/Plants/Evaluation/{plantId}",
            $"/Plants/Compare?leftId={plantId}&rightId={secondId}", "/Plants/Compare?leftId=999999",
            "/Plants/Details/1", "/CropJournal/Edit/1", "/CropJournal/Delete/1",
            "/CarePlans/Edit/1", "/CarePlans/Delete/1",
            "/Materials", "/Materials/Create", "/CropJournal", "/CropJournal/Create"
        })
            check((await client.GetAsync(path)).StatusCode == HttpStatusCode.OK, "Render simplified route: " + path);

        // Materials catalog CRUD
        check((await Post("/Materials/Create", new() { ["Name"] = "Phân gà", ["Category"] = "Fertilizer", ["DefaultUnit"] = "bao", ["Notes"] = "Bón lót" })).StatusCode == HttpStatusCode.Redirect,
            "Create material Phân gà");
        check((await Post("/Materials/Create", new() { ["Name"] = "Phân bò", ["Category"] = "Fertilizer", ["DefaultUnit"] = "kg" })).StatusCode == HttpStatusCode.Redirect,
            "Create material Phân bò");
        check((await Post("/Materials/Create", new() { ["Name"] = "Thuốc trừ sâu Nhật", ["Category"] = "Pesticide", ["DefaultUnit"] = "ml" })).StatusCode == HttpStatusCode.Redirect,
            "Create material Thuốc trừ sâu Nhật");
        check((await Post("/Materials/Create", new() { ["Name"] = "Dây buộc giàn", ["Category"] = "Other", ["DefaultUnit"] = "cuộn" })).StatusCode == HttpStatusCode.Redirect,
            "Create material Dây buộc giàn");
        check((await Post("/Materials/Create", new() { ["Name"] = "phân gà", ["Category"] = "Fertilizer" })).StatusCode == HttpStatusCode.OK,
            "Duplicate material name (case-insensitive) is rejected");

        await using (var db = new ApplicationDbContext(options))
        {
            chickenManureId = (await db.MaterialItems.SingleAsync(m => m.Name == "Phân gà")).Id;
            cowManureId = (await db.MaterialItems.SingleAsync(m => m.Name == "Phân bò")).Id;
            jpPesticideId = (await db.MaterialItems.SingleAsync(m => m.Name == "Thuốc trừ sâu Nhật")).Id;
            unusedMatId = (await db.MaterialItems.SingleAsync(m => m.Name == "Dây buộc giàn")).Id;
        }

        check((await Post($"/Materials/Delete/{unusedMatId}", new() { ["id"] = unusedMatId.ToString() })).StatusCode == HttpStatusCode.Redirect,
            "Delete unused material succeeds");
        await using (var db = new ApplicationDbContext(options))
            check(!await db.MaterialItems.AnyAsync(m => m.Id == unusedMatId), "Deleted unused material is hidden from catalog");

        // Care plans
        Dictionary<string, string> Plan(string title = "Kiểm tra cây sống") => new()
        {
            ["PlantId"] = plantId.ToString(),
            ["Title"] = title,
            ["Kind"] = "Observation",
            ["PlannedDay"] = "7",
            ["Instructions"] = "Đếm 100 cây đại diện và ghi tình trạng lá"
        };
        var planRaces = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/CarePlans/Create", Plan(), $"/CarePlans/Create?plantId={plantId}")));
        check(planRaces.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1 && planRaces.Count(r => r.StatusCode == HttpStatusCode.OK) == 1,
            "Concurrent identical care plans are serialized");
        await using (var db = new ApplicationDbContext(options))
            stepId = (await db.CropCareSteps.SingleAsync(s => s.PlantId == plantId)).Id;

        List<KeyValuePair<string, string>> EntryPairs(string kind = "Observation", string date = "2024-01-08", int[]? workers = null, int? materialItemId = null)
        {
            var list = new List<KeyValuePair<string, string>>
            {
                new("PlantId", plantId.ToString()),
                new("RecordCode", Guid.NewGuid().ToString("N")),
                new("Date", date),
                new("Kind", kind)
            };
            if (materialItemId.HasValue) list.Add(new("MaterialItemId", materialItemId.Value.ToString()));
            foreach (var wid in workers ?? new[] { w1Id })
                list.Add(new("WorkerIds", wid.ToString()));
            return list;
        }

        Task<HttpResponseMessage> AddPairs(List<KeyValuePair<string, string>> fields) =>
            PostPairs("/CropJournal/Create", fields, $"/CropJournal/Create?plantId={plantId}");

        // Concurrent duplicate journal with 1 worker and no summary/metrics
        var observation = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        observation.Add(new("CareStepId", stepId.ToString()));
        observation.Add(new("HeightCm", "12"));
        observation.Add(new("SurvivalPercent", "95"));
        var same = observation.ToList();
        var races = await Task.WhenAll(AddPairs(observation), AddPairs(same));
        check(races.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1, "Double-submitted daily work is written once under shared transaction lock");

        int observationId;
        var obsCode = observation.First(x => x.Key == "RecordCode").Value;
        await using (var db = new ApplicationDbContext(options))
        {
            observationId = (await db.CropJournalEntries.SingleAsync(e => e.RecordCode == obsCode)).Id;
            var m = await new CropSeasonService(db).LoadAsync(plantId);
            check(m!.DoneSteps == 1 && m.LatestObservation?.HeightCm == 12 && m.LatestObservation.SurvivalPercent == 95,
                "Minimal observation with assigned worker marks plan complete and keeps measurements");
            check(m.Timeline.Any(t => t.Title.Contains("sổ cũ") && t.Description!.Contains("Dữ liệu cũ phải giữ")),
                "Existing irrigation logs remain in the unified timeline");
        }

        // Validation rules on simplified work form
        var wrongSeason = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        wrongSeason.RemoveAll(x => x.Key == "PlantId");
        wrongSeason.Add(new("PlantId", secondId.ToString()));
        wrongSeason.Add(new("CareStepId", stepId.ToString()));
        check((await AddPairs(wrongSeason)).StatusCode == HttpStatusCode.OK, "Plan references cannot cross seasons");

        check((await AddPairs(EntryPairs("Observation", "2023-12-31", new[] { w1Id }))).StatusCode == HttpStatusCode.OK,
            "Journal cannot predate planting");

        var missingPlant = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        missingPlant.RemoveAll(x => x.Key == "PlantId");
        missingPlant.Add(new("PlantId", "999999"));
        check((await AddPairs(missingPlant)).StatusCode == HttpStatusCode.OK, "Journal rejects a missing lot");

        var badSurvival = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        badSurvival.Add(new("SurvivalPercent", "101"));
        check((await AddPairs(badSurvival)).StatusCode == HttpStatusCode.OK, "MVC enforces growth percentage validation");

        check((await AddPairs(EntryPairs("Care", "2024-01-08", Array.Empty<int>()))).StatusCode == HttpStatusCode.OK,
            "Reject daily work with zero workers");
        check((await AddPairs(EntryPairs("Care", "2024-01-08", new[] { 999999 }))).StatusCode == HttpStatusCode.OK,
            "Reject daily work with nonexistent worker");
        check((await AddPairs(EntryPairs("Care", "2024-01-08", new[] { wInactiveId }))).StatusCode == HttpStatusCode.OK,
            "Reject daily work assigning an inactive worker");

        check((await AddPairs(EntryPairs("Fertilize", "2024-01-08", new[] { w1Id }, null))).StatusCode == HttpStatusCode.OK,
            "Fertilize requires selecting a material from catalog");
        check((await AddPairs(EntryPairs("Fertilize", "2024-01-08", new[] { w1Id }, jpPesticideId))).StatusCode == HttpStatusCode.OK,
            "Fertilize rejects pesticide material");

        check((await client.PostAsync("/CropJournal/Create", new FormUrlEncodedContent(EntryPairs()))).StatusCode == HttpStatusCode.BadRequest,
            "Journal writes require antiforgery tokens");

        // CarePlan immutability & legacy repair checks
        var editPlan = Plan("Đổi kế hoạch sau khi làm");
        editPlan["Id"] = stepId.ToString();
        check((await Post($"/CarePlans/Edit/{stepId}", editPlan)).StatusCode == HttpStatusCode.OK,
            "Plan with actual journals cannot be rewritten to manufacture adherence");
        await Post($"/CarePlans/Delete/{stepId}", new() { ["id"] = stepId.ToString() });
        await using (var db = new ApplicationDbContext(options))
            check(await db.CropCareSteps.AnyAsync(s => s.Id == stepId), "Used care plan cannot be deleted through a forged request");

        await using (var db = new ApplicationDbContext(options))
        {
            var broken = await db.CropCareSteps.FindAsync(stepId);
            broken!.PlannedDay = null;
            await db.SaveChangesAsync();
            var notebook = await new CropSeasonService(db).LoadAsync(plantId);
            check(notebook!.IncompleteSteps == 1 && notebook.DoneSteps == 0, "Incomplete soft plan cannot appear as a valid completed procedure");
        }
        var repairPlan = Plan();
        repairPlan["Id"] = stepId.ToString();
        check((await Post($"/CarePlans/Edit/{stepId}", repairPlan)).StatusCode == HttpStatusCode.Redirect,
            "Incomplete legacy plan can be repaired without rewriting valid prior plans");

        // Multi-worker daily work with Phân gà (no amount, no summary needed) + Water + Care
        var fertMulti = EntryPairs("Fertilize", "2024-01-10", new[] { w1Id, w2Id }, chickenManureId);
        var fertCode = fertMulti.First(x => x.Key == "RecordCode").Value;
        check((await AddPairs(fertMulti)).StatusCode == HttpStatusCode.Redirect,
            "Record Fertilize with Phân gà and 2 workers without requiring amount or summary");

        check((await AddPairs(EntryPairs("Water", "2024-01-11", new[] { w1Id }))).StatusCode == HttpStatusCode.Redirect,
            "Record Water without WaterResourceId or amount");
        check((await AddPairs(EntryPairs("Care", "2024-01-12", new[] { w1Id, w2Id }))).StatusCode == HttpStatusCode.Redirect,
            "Record Care without material");

        int fertEntryId;
        await using (var db = new ApplicationDbContext(options))
        {
            var fe = await db.CropJournalEntries.SingleAsync(e => e.RecordCode == fertCode);
            fertEntryId = fe.Id;
            var assigned = await db.CropJournalWorkers.Where(w => w.CropJournalEntryId == fertEntryId).Select(w => w.WorkerId).ToListAsync();
            check(fe.Material == "Phân gà" && fe.MaterialItemId == chickenManureId && assigned.Count == 2 && assigned.Contains(w1Id) && assigned.Contains(w2Id),
                "Multi-worker assignments and selected material snapshot are persisted");
        }

        // Verify filtering & display on /CropJournal, /Plants/Details, /Workers/Details
        var dailyHtml = WebUtility.HtmlDecode(await client.GetStringAsync("/CropJournal"));
        check(dailyHtml.Contains("Phân gà") && dailyHtml.Contains("Nguyễn Văn A") && dailyHtml.Contains("Trần Thị B"),
            "Daily work list renders selected material and all assigned workers");
        var filteredWorker = WebUtility.HtmlDecode(await client.GetStringAsync($"/CropJournal?workerId={w2Id}&materialItemId={chickenManureId}"));
        check(filteredWorker.Contains("Phân gà") && filteredWorker.Contains("Trần Thị B"),
            "Daily work filters by worker and material");
        var plantDetailsHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Plants/Details/{plantId}"));
        check(plantDetailsHtml.Contains("Phân gà") && plantDetailsHtml.Contains("Nguyễn Văn A") && plantDetailsHtml.Contains("Trần Thị B"),
            "Season details page renders assigned workers and catalog material");
        var workerDetailsHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Workers/Details/{w2Id}"));
        check(workerDetailsHtml.Contains("Phân gà") && workerDetailsHtml.Contains("2 công việc được gán"),
            "Worker details page displays assigned work history");

        // Used material and assigned worker cannot be deleted
        await Post($"/Materials/Delete/{chickenManureId}", new() { ["id"] = chickenManureId.ToString() });
        await Post($"/Workers/Delete/{w2Id}", new() { ["Id"] = w2Id.ToString() });
        await using (var db = new ApplicationDbContext(options))
        {
            check(await db.MaterialItems.AnyAsync(m => m.Id == chickenManureId), "Used material cannot be deleted");
            check(await db.Workers.AnyAsync(w => w.Id == w2Id), "Worker with assigned daily work cannot be deleted");
        }

        // Editing an existing work remains allowed even if a previously assigned worker later becomes inactive
        await using (var db = new ApplicationDbContext(options))
        {
            (await db.Workers.FindAsync(w2Id))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        var editMulti = EntryPairs("Fertilize", "2024-01-10", new[] { w1Id, w2Id }, cowManureId);
        editMulti.Add(new("Id", fertEntryId.ToString()));
        editMulti.Add(new("Amount", "3"));
        editMulti.Add(new("Unit", "bao"));
        check((await PostPairs($"/CropJournal/Edit/{fertEntryId}", editMulti)).StatusCode == HttpStatusCode.Redirect,
            "Editing existing work preserves previously assigned worker even after worker leaves");
        await using (var db = new ApplicationDbContext(options))
        {
            (await db.Workers.FindAsync(w2Id))!.IsActive = true;
            await db.SaveChangesAsync();
        }

        // Pesticide withdrawal checks: optional when omitted, enforced when entered
        var sprayNoDays = EntryPairs("Pesticide", "2024-01-20", new[] { w1Id }, jpPesticideId);
        check((await AddPairs(sprayNoDays)).StatusCode == HttpStatusCode.Redirect,
            "Pesticide journal succeeds without optional withdrawal days");

        var sprayWithDays = EntryPairs("Pesticide", "2024-02-01", new[] { w1Id }, jpPesticideId);
        sprayWithDays.Add(new("Amount", "30"));
        sprayWithDays.Add(new("Unit", "ml"));
        sprayWithDays.Add(new("WithdrawalDays", "7"));
        check((await AddPairs(sprayWithDays)).StatusCode == HttpStatusCode.Redirect,
            "Pesticide journal persists explicit withdrawal interval");

        var harvestBefore = EntryPairs("Harvest", "2024-02-07", new[] { w1Id });
        harvestBefore.Add(new("Amount", "35"));
        harvestBefore.Add(new("Unit", "kg"));
        check((await AddPairs(harvestBefore)).StatusCode == HttpStatusCode.OK,
            "Harvest before explicit withdrawal deadline is blocked");

        var harvestOn = EntryPairs("Harvest", "2024-02-08", new[] { w1Id });
        harvestOn.Add(new("Amount", "35"));
        harvestOn.Add(new("Unit", "kg"));
        check((await AddPairs(harvestOn)).StatusCode == HttpStatusCode.Redirect,
            "Harvest on withdrawal deadline can be recorded");

        var backdated = EntryPairs("Pesticide", "2024-02-05", new[] { w1Id }, jpPesticideId);
        backdated.Add(new("WithdrawalDays", "10"));
        check((await AddPairs(backdated)).StatusCode == HttpStatusCode.OK,
            "Backdated pesticide with withdrawal interval cannot invalidate a recorded later harvest");

        var later = EntryPairs("Observation", "2024-02-10", new[] { w1Id });
        later.Add(new("HeightCm", "35"));
        later.Add(new("GrowthStage", "Flowering"));
        check((await AddPairs(later)).StatusCode == HttpStatusCode.Redirect,
            "Later growth observation may leave unmeasured survival unknown");

        await using (var db = new ApplicationDbContext(options))
        {
            var m = await new CropSeasonService(db).LoadAsync(plantId);
            check(m!.HarvestKg == 35 && m.LatestObservation?.HeightCm == 35 && m.LatestObservation.SurvivalPercent == null,
                "Latest growth does not borrow an older survival estimate; harvest aggregates recorded kg");
        }

        // Evaluation, compare, clone plan, delete guards
        await Post($"/Plants/Evaluation/{plantId}", new()
        {
            ["Id"] = plantId.ToString(),
            ["CareProtocol"] = "Theo dõi đất trước tưới",
            ["SeasonReview"] = "Góc trũng vàng lá sau mưa",
            ["NextSeasonChanges"] = "Làm rãnh thoát nước trước vụ sau"
        });
        var comparison = WebUtility.HtmlDecode(await client.GetStringAsync($"/Plants/Compare?leftId={plantId}&rightId={secondId}"));
        check(comparison.Contains("Làm rãnh thoát nước") && comparison.Contains("chưa quy đổi theo diện tích"),
            "Season comparison shows farmer lessons without claiming causal superiority");

        var clone = new Dictionary<string, string>
        {
            ["BatchCode"] = "SEASON-CLONE",
            ["SeasonName"] = "Vụ mới có điều chỉnh",
            ["AreaId"] = areaId.ToString(),
            ["PlantTypeId"] = typeId.ToString(),
            ["SeedId"] = seedId.ToString(),
            ["PlantingDate"] = "2025-01-01",
            ["Status"] = "Growing",
            ["SourcePlantId"] = plantId.ToString()
        };
        check((await Post("/Plants/Create", clone)).StatusCode == HttpStatusCode.Redirect, "New season can reuse a previous compatible plan");
        await using (var db = new ApplicationDbContext(options))
        {
            var copy = await db.Plants.SingleAsync(p => p.BatchCode == "SEASON-CLONE");
            check(await db.CropCareSteps.CountAsync(s => s.PlantId == copy.Id) == 1 && !await db.CropJournalEntries.AnyAsync(e => e.PlantId == copy.Id),
                "Plan reuse copies intentions but never old growth, photos or harvest outcomes");
        }

        await Post($"/Plants/Delete/{plantId}", new() { ["id"] = plantId.ToString() });
        await using (var db = new ApplicationDbContext(options))
            check(await db.Plants.AnyAsync(p => p.Id == plantId), "Season with journals is protected against deletion in the backend");

        var changedDate = clone.ToDictionary(x => x.Key, x => x.Value);
        changedDate["Id"] = plantId.ToString();
        changedDate["BatchCode"] = "SEASON-QA-01";
        changedDate["PlantingDate"] = "2024-03-01";
        check((await Post($"/Plants/Edit/{plantId}", changedDate)).StatusCode == HttpStatusCode.OK,
            "Planting date cannot be moved past existing care journals");

        await PhotoChecks();

        var changedObs = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        changedObs.Add(new("Id", observationId.ToString()));
        changedObs.Add(new("CareStepId", stepId.ToString()));
        changedObs.Add(new("HeightCm", "13"));
        check((await PostPairs($"/CropJournal/Edit/{observationId}", changedObs)).StatusCode == HttpStatusCode.Redirect,
            "A mistaken growth measurement can be corrected without requiring ChangeReason");

        changedObs.RemoveAll(x => x.Key == "PlantId");
        changedObs.Add(new("PlantId", secondId.ToString()));
        check((await PostPairs($"/CropJournal/Edit/{observationId}", changedObs)).StatusCode == HttpStatusCode.OK,
            "Existing journal cannot be moved to another valid season");

        await Post($"/CropJournal/Delete/{observationId}", new() { ["id"] = observationId.ToString() });
        await using (var db = new ApplicationDbContext(options))
            check((await new CropSeasonService(db).LoadAsync(plantId))!.DoneSteps == 0,
                "Deleting actual evidence returns its care plan to unrecorded");

        var restore = EntryPairs("Observation", "2024-01-08", new[] { w1Id });
        restore.Add(new("CareStepId", stepId.ToString()));
        await AddPairs(restore);

        var simSpray = EntryPairs("Pesticide", "2024-06-03", new[] { w1Id }, jpPesticideId);
        simSpray.RemoveAll(x => x.Key == "PlantId");
        simSpray.Add(new("PlantId", secondId.ToString()));
        simSpray.Add(new("WithdrawalDays", "7"));
        var simHarvest = EntryPairs("Harvest", "2024-06-04", new[] { w1Id });
        simHarvest.RemoveAll(x => x.Key == "PlantId");
        simHarvest.Add(new("PlantId", secondId.ToString()));
        var concurrentSafety = await Task.WhenAll(AddPairs(simSpray), AddPairs(simHarvest));
        check(concurrentSafety.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1 && concurrentSafety.Count(r => r.StatusCode == HttpStatusCode.OK) == 1,
            "Concurrent pesticide and conflicting harvest writes cannot both succeed");

        check((await Post("/CarePlans/Create", new() { ["PlantId"] = plantId.ToString() }, $"/CarePlans/Create?plantId={plantId}")).StatusCode == HttpStatusCode.OK,
            "Incomplete plan form returns validation rather than throwing on NULL title");

        var remove = EntryPairs("Care", "2024-01-08", new[] { w1Id });
        var removeCode = remove.First(x => x.Key == "RecordCode").Value;
        await AddPairs(remove);
        int removeId;
        await using (var db = new ApplicationDbContext(options))
            removeId = (await db.CropJournalEntries.SingleAsync(e => e.RecordCode == removeCode)).Id;
        await Post($"/CropJournal/Delete/{removeId}", new() { ["id"] = removeId.ToString() });
        await using (var db = new ApplicationDbContext(options))
            check(!await db.CropJournalEntries.AnyAsync(e => e.Id == removeId), "Mistaken actual journal can be deleted through confirmation flow");

        async Task PhotoChecks()
        {
            var e = EntryPairs("Care", "2024-01-08", new[] { w1Id });
            var code = e.First(x => x.Key == "RecordCode").Value;
            var html = await client.GetStringAsync($"/CropJournal/Create?plantId={plantId}");
            var token = Csrf(html);
            var rev = Regex.Match(html, "name=\"OriginalRevision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

            using var invalid = new MultipartFormDataContent();
            invalid.Add(new StringContent(token), "__RequestVerificationToken");
            if (rev.Length > 0) invalid.Add(new StringContent(rev), "OriginalRevision");
            foreach (var pair in e) invalid.Add(new StringContent(pair.Value), pair.Key);
            invalid.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>")), "Photo", "fake.jpg");
            check((await client.PostAsync("/CropJournal/Create", invalid)).StatusCode == HttpStatusCode.OK,
                "Upload rejects executable content disguised as a photo");

            using var valid = new MultipartFormDataContent();
            valid.Add(new StringContent(token), "__RequestVerificationToken");
            if (rev.Length > 0) valid.Add(new StringContent(rev), "OriginalRevision");
            foreach (var pair in e) valid.Add(new StringContent(pair.Value), pair.Key);
            valid.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRz0AAAAASUVORK5CYII=")), "Photo", "photo.png");
            check((await client.PostAsync("/CropJournal/Create", valid)).StatusCode == HttpStatusCode.Redirect,
                "Upload accepts a PNG photo and stores an application-generated filename");

            await using var db = new ApplicationDbContext(options);
            var saved = await db.CropJournalEntries.SingleAsync(j => j.RecordCode == code);
            check(CropSeasonRules.PhotoIsLocal(saved.PhotoPath) && (await client.GetAsync(saved.PhotoPath)).StatusCode == HttpStatusCode.OK,
                "Uploaded crop photo is accessible with a safe local path");
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Farm-Management.sln"))) root = root.Parent;
            var path = Path.Combine(root!.FullName, "Farm-Management", "wwwroot", "uploads", "crops", Path.GetFileName(saved.PhotoPath!));
            File.Delete(path);
            saved.PhotoPath = null;
            await db.SaveChangesAsync();
        }

        async Task<HttpResponseMessage> Post(string route, Dictionary<string, string> fields, string? tokenRoute = null)
        {
            var html = await client.GetStringAsync(tokenRoute ?? route);
            fields["__RequestVerificationToken"] = Csrf(html);
            TestChangeFields.Apply(fields, html);
            return await client.PostAsync(route, new FormUrlEncodedContent(fields));
        }

        async Task<HttpResponseMessage> PostPairs(string route, List<KeyValuePair<string, string>> fields, string? tokenRoute = null)
        {
            var html = await client.GetStringAsync(tokenRoute ?? route);
            var payload = new List<KeyValuePair<string, string>>(fields)
            {
                new("__RequestVerificationToken", Csrf(html))
            };
            var rev = Regex.Match(html, "name=\"OriginalRevision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            if (rev.Length > 0 && payload.All(x => x.Key != "OriginalRevision"))
                payload.Add(new("OriginalRevision", rev));
            return await client.PostAsync(route, new FormUrlEncodedContent(payload));
        }

        static string Csrf(string html)
        {
            var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            return token.Length > 0 ? token : throw new Exception("Missing season antiforgery token");
        }
    }
}
