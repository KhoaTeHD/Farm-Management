using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class ProvenanceChecks
{
    public static async Task Integration(HttpClient client,DbContextOptions<ApplicationDbContext> options,Action<bool,string> check)
    {
        int areaId,plantId,inputId,waterId,soilId,entryId,harvestId,traceId;
        await using(var db=new ApplicationDbContext(options))
        {
            var area=new Area{Name="Vùng truy xuất QA"};var type=new PlantType{Name="Cây truy xuất QA"};db.AddRange(area,type);await db.SaveChangesAsync();
            var seed=new Seed{Name="Giống truy xuất QA",PlantTypeId=type.Id,Origin="Nguồn giống QA"};db.Add(seed);await db.SaveChangesAsync();
            var p=new Plant{BatchCode="TRACE-PLANT",SeasonName="Vụ truy xuất kiểm thử",AreaId=area.Id,PlantTypeId=type.Id,SeedId=seed.Id,PlantingDate=new(2024,1,1),Status="Growing"};db.Add(p);await db.SaveChangesAsync();areaId=area.Id;plantId=p.Id;
        }
        foreach(var path in new[]{"/FarmRecords","/FarmRecords/CreateInput","/FarmRecords/CreateResource","/FarmRecords/CreateInspection","/RecordHistory","/RecordHistory/Archived","/HarvestTrace"})
            check((await client.GetAsync(path)).StatusCode==HttpStatusCode.OK,"Render provenance screen with nullable existing rows: "+path);
        Dictionary<string,string> Input(string code="TRACE-INPUT")=>new(){["Code"]=code,["Kind"]="Fertilizer",["ProductName"]="Phân kiểm thử có nguồn gốc",["ManufacturerBatch"]="NSX-QA-001",["Supplier"]="Nơi mua QA",["InvoiceNumber"]="HD-QA-001",["ReceivedDate"]="2023-12-01",["ExpiryDate"]="2025-12-31",["Quantity"]="10",["Unit"]="kg",["Status"]="Released"};
        check((await Post("/FarmRecords/CreateInput",Input())).StatusCode==HttpStatusCode.Redirect,"Create an input lot with supplier, manufacturer batch and invoice");
        await using(var db=new ApplicationDbContext(options))inputId=(await db.InputLots.SingleAsync(l=>l.Code=="TRACE-INPUT")).Id;
        Dictionary<string,string> Resource(string kind)=>new(){["AreaId"]=areaId.ToString(),["Kind"]=kind,["Name"]=kind=="Water"?"Giếng truy xuất QA":"Đất vườn truy xuất QA",["Location"]="Phía đông vườn QA",["RiskAssessment"]="Kiểm tra nguồn thải và dòng chảy",["Treatment"]="Theo dõi và kiểm nghiệm",["Status"]="Released"};
        check((await Post("/FarmRecords/CreateResource",Resource("Water"))).StatusCode==HttpStatusCode.Redirect,"Create water source tied to its cultivation area");
        await Post("/FarmRecords/CreateResource",Resource("Soil"));
        await using(var db=new ApplicationDbContext(options)){waterId=(await db.FieldResources.SingleAsync(r=>r.Name=="Giếng truy xuất QA")).Id;soilId=(await db.FieldResources.SingleAsync(r=>r.Name=="Đất vườn truy xuất QA")).Id;}
        Dictionary<string,string> Inspection(string type,int id,string conclusion,string date="2024-01-01")=>new(){["TargetType"]=type,["TargetId"]=id.ToString(),["ReportNumber"]="PKN-"+Guid.NewGuid().ToString("N")[..8],["Laboratory"]="Phòng kiểm nghiệm QA",["SampleDate"]=date,["ReportDate"]=date,["Indicator"]="Chỉ tiêu QA",["Result"]="Giá trị theo phiếu QA",["LimitReference"]="Tài liệu và ngưỡng trên phiếu QA",["Conclusion"]=conclusion};
        check((await Post("/FarmRecords/CreateInspection",Inspection("FieldResource",waterId,"Fail"))).StatusCode==HttpStatusCode.Redirect,"Record actual failed water inspection");
        await using(var db=new ApplicationDbContext(options))check((await db.FieldResources.FindAsync(waterId))!.Status=="Hold","Failed inspection atomically holds the water source");
        var release=Resource("Water");release["Id"]=waterId.ToString();
        check((await Post($"/FarmRecords/EditResource/{waterId}",release)).StatusCode==HttpStatusCode.OK,"Operator cannot release a source while latest inspection still fails");
        check((await Post("/FarmRecords/CreateInspection",Inspection("FieldResource",waterId,"Pass","2024-01-02"))).StatusCode==HttpStatusCode.Redirect,"A later corrective inspection is added without deleting the failed result");
        await using(var db=new ApplicationDbContext(options))check((await db.FieldResources.FindAsync(waterId))!.Status=="Hold","A passing report does not silently release a previously held source");
        release.Remove("OriginalRevision");check((await Post($"/FarmRecords/EditResource/{waterId}",release)).StatusCode==HttpStatusCode.Redirect,"Explicit release succeeds after a newer passing report for the same indicator");
        var stale=await client.GetStringAsync($"/FarmRecords/EditInput/{inputId}");var first=Input();first["Id"]=inputId.ToString();first["Notes"]="Lần sửa thứ nhất";TestChangeFields.Apply(first,stale);
        var second=Input();second["Id"]=inputId.ToString();second["Notes"]="Lần sửa thứ hai";TestChangeFields.Apply(second,stale);
        var editRace=await Task.WhenAll(Post($"/FarmRecords/EditInput/{inputId}",first),Post($"/FarmRecords/EditInput/{inputId}",second));
        check(editRace.Count(r=>r.StatusCode==HttpStatusCode.Redirect)==1 && editRace.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1,"Stale concurrent edits cannot overwrite an already revised input record");
        var noReason=Input();noReason["Id"]=inputId.ToString();noReason["ChangeReason"]="";
        check((await Post($"/FarmRecords/EditInput/{inputId}",noReason)).StatusCode==HttpStatusCode.BadRequest,"Changes require a reason even when forging an HTTP request");
        var noActor=Input("NO-ACTOR");noActor["ChangedBy"]="";
        check((await Post("/FarmRecords/CreateInput",noActor)).StatusCode==HttpStatusCode.BadRequest,"Provenance writes require a named self-declared operator");
        Dictionary<string,string> Journal(string kind="Fertilize",string amount="6",string date="2024-02-01")=>new(){["PlantId"]=plantId.ToString(),["RecordCode"]=Guid.NewGuid().ToString("N"),["Date"]=date,["Kind"]=kind,["Recorder"]="Người làm QA",["Summary"]="Bón theo lượng thực tế đã cân",["InputLotId"]=inputId.ToString(),["WaterResourceId"]=waterId.ToString(),["Material"]="Nội dung tùy ý không được thay nguồn thật",["Amount"]=amount,["Unit"]=kind=="Water"?"l":"kg",["Quality"]="Chất lượng QA"};
        var quota=await Task.WhenAll(Post("/CropJournal/Create",Journal(),$"/CropJournal/Create?plantId={plantId}"),Post("/CropJournal/Create",Journal(),$"/CropJournal/Create?plantId={plantId}"));
        check(quota.Count(r=>r.StatusCode==HttpStatusCode.Redirect)==1 && quota.Count(r=>r.StatusCode==HttpStatusCode.OK)==1,"Concurrent consumption cannot exceed the recorded input quantity");
        await using(var db=new ApplicationDbContext(options))
        {var e=await db.CropJournalEntries.SingleAsync(e=>e.PlantId==plantId);entryId=e.Id;check(e.Material=="Phân kiểm thử có nguồn gốc" && e.InputLotId==inputId,"Journal material name comes from the selected input lot, preserving provenance");}
        var wrong=Journal();wrong["InputLotId"]="999999";check((await Post("/CropJournal/Create",wrong,$"/CropJournal/Create?plantId={plantId}")).StatusCode==HttpStatusCode.OK,"Journal rejects a missing input reference");
        var waterLog=Journal("Water","20");check((await Post("/CropJournal/Create",waterLog,$"/CropJournal/Create?plantId={plantId}")).StatusCode==HttpStatusCode.Redirect,"Water journal links the checked source for its area");
        await Post("/FarmRecords/CreateInspection",Inspection("FieldResource",waterId,"Pending","2024-01-03"));
        check((await Post("/CropJournal/Create",Journal("Water","20"),$"/CropJournal/Create?plantId={plantId}")).StatusCode==HttpStatusCode.OK,"Pending latest inspection blocks subsequent water records");
        await Post("/FarmRecords/CreateInspection",Inspection("FieldResource",waterId,"Pass","2024-01-04"));release.Remove("OriginalRevision");await Post($"/FarmRecords/EditResource/{waterId}",release);
        var unused=Input("UNUSED-ARCHIVE");await Post("/FarmRecords/CreateInput",unused);int unusedId;
        await using(var db=new ApplicationDbContext(options))unusedId=(await db.InputLots.SingleAsync(l=>l.Code=="UNUSED-ARCHIVE")).Id;
        await Post($"/RecordHistory/Archive/{unusedId}",new(){["recordType"]="InputLot"},$"/RecordHistory/Archive/{unusedId}?recordType=InputLot");
        await using(var db=new ApplicationDbContext(options))check(!await db.InputLots.AnyAsync(l=>l.Id==unusedId) && await db.InputLots.IgnoreQueryFilters().AnyAsync(l=>l.Id==unusedId && l.IsDeleted==true),"Archive hides an unused record while retaining its physical data");
        await Post($"/RecordHistory/Restore/{unusedId}",new(){["recordType"]="InputLot"},$"/RecordHistory/Restore/{unusedId}?recordType=InputLot");
        await using(var db=new ApplicationDbContext(options))check(await db.InputLots.AnyAsync(l=>l.Id==unusedId),"Archived input can be restored through an audited confirmation");
        await Post($"/RecordHistory/Archive/{inputId}",new(){["recordType"]="InputLot"},$"/RecordHistory/Archive/{inputId}?recordType=InputLot");
        await using(var db=new ApplicationDbContext(options))check(await db.InputLots.AnyAsync(l=>l.Id==inputId),"Used input cannot be archived away from its traceability links");
        var harvest=Journal("Harvest","35","2024-02-20");check((await Post("/CropJournal/Create",harvest,$"/CropJournal/Create?plantId={plantId}")).StatusCode==HttpStatusCode.Redirect,"Harvest automatically generates a traceable batch record");
        string code,savedJson;int initialRevision;
        await using(var db=new ApplicationDbContext(options))
        {
            harvestId=(await db.CropJournalEntries.SingleAsync(e=>e.RecordCode==harvest["RecordCode"])).Id;
            var t=await db.HarvestTraces.SingleAsync(t=>t.JournalEntryId==harvestId);traceId=t.Id;code=t.Code!;savedJson=t.SnapshotJson!;initialRevision=t.Revision!.Value;
            var snapshot=HarvestTraceService.Read(t);check(snapshot!=null && snapshot.Inputs.Any(l=>l.InvoiceNumber=="HD-QA-001") && snapshot.Resources.Any(r=>r.Id==soilId) && snapshot.Reports.Any(),"Trace snapshot includes supplier invoice, soil/water and inspection provenance");
        }
        var traceHtml=WebUtility.HtmlDecode(await client.GetStringAsync($"/HarvestTrace/Details/{traceId}"));check(traceHtml.Contains(code) && traceHtml.Contains("HD-QA-001"),"Trace screen displays the saved provenance under its stable batch code");
        var correction=Journal();correction["Id"]=entryId.ToString();correction["Summary"]="Đã sửa ghi chú sau đối chiếu sổ giấy";
        check((await Post($"/CropJournal/Edit/{entryId}",correction)).StatusCode==HttpStatusCode.Redirect,"Care correction remains possible with audit evidence");
        traceHtml=WebUtility.HtmlDecode(await client.GetStringAsync($"/HarvestTrace/Details/{traceId}"));check(traceHtml.Contains("đã thay đổi so với bản lưu"),"Trace detects source changes without overwriting its existing snapshot");
        await using(var db=new ApplicationDbContext(options))check((await db.HarvestTraces.FindAsync(traceId))!.SnapshotJson==savedJson,"Original trace snapshot remains intact after source edits");
        await Post($"/HarvestTrace/Reissue/{traceId}",new());
        int traceAuditId;
        await using(var db=new ApplicationDbContext(options))
        {
            var t=await db.HarvestTraces.FindAsync(traceId);check(t!.Code==code && t.Revision>initialRevision && t.SnapshotJson!=savedJson,"Explicit reissue preserves the code and stores a revised snapshot");
            var a=await db.CultivationAudits.Where(a=>a.RecordType=="HarvestTrace" && a.RecordId==traceId && a.Action=="Updated").OrderByDescending(a=>a.Id).FirstAsync();traceAuditId=a.Id;
            check(a.BeforeJson!.Contains("HD-QA-001") && a.AfterJson!.Contains("HD-QA-001") && a.Actor=="Người kiểm thử QA" && !string.IsNullOrWhiteSpace(a.Reason),"Audit retains before/after trace data, operator and reason");
        }
        check((await client.GetAsync($"/RecordHistory/TraceVersion/{traceAuditId}?side=Before")).StatusCode==HttpStatusCode.OK && (await client.GetAsync($"/RecordHistory/TraceVersion/{traceAuditId}?side=After")).StatusCode==HttpStatusCode.OK,"Both historical trace versions can be viewed");
        await Post($"/CropJournal/Delete/{harvestId}",new());
        await using(var db=new ApplicationDbContext(options))check(await db.CropJournalEntries.IgnoreQueryFilters().AnyAsync(e=>e.Id==harvestId && e.IsDeleted==true) && (await db.HarvestTraces.FindAsync(traceId))!.IsVoided==true,"Archiving a harvest retains its data and suspends its trace code");
        await Post($"/RecordHistory/Restore/{harvestId}",new(){["recordType"]="CropJournalEntry"},$"/RecordHistory/Restore/{harvestId}?recordType=CropJournalEntry");
        await using(var db=new ApplicationDbContext(options))check(await db.CropJournalEntries.AnyAsync(e=>e.Id==harvestId) && (await db.HarvestTraces.FindAsync(traceId))!.IsVoided==false,"Valid harvest restoration restores its trace status without losing history");
        await Post("/FarmRecords/CreateInspection",Inspection("HarvestTrace",traceId,"Fail","2024-02-21"));
        await using(var db=new ApplicationDbContext(options))check((await db.HarvestTraces.FindAsync(traceId))!.IsVoided==true,"Failed product inspection suspends the harvest trace");
        await Post($"/HarvestTrace/Reissue/{traceId}",new());
        await using(var db=new ApplicationDbContext(options))check((await db.HarvestTraces.FindAsync(traceId))!.IsVoided==true,"Reissue cannot reactivate a trace with a failing latest product inspection");
        await Post("/FarmRecords/CreateInspection",Inspection("HarvestTrace",traceId,"Pass","2024-02-22"));await Post($"/HarvestTrace/Reissue/{traceId}",new());
        await using(var db=new ApplicationDbContext(options))check((await db.HarvestTraces.FindAsync(traceId))!.IsVoided==false,"New passing result plus explicit reissue can reactivate the trace");
        await using(var db=new ApplicationDbContext(options))
        {
            var a=await db.CultivationAudits.FirstAsync(a=>a.RecordType=="InputLot" && a.RecordId==inputId);a.Reason="tamper";
            try{await db.SaveChangesAsync();throw new Exception("Audit mutation unexpectedly succeeded");}catch(InvalidOperationException ex)when(ex.Message.Contains("Lịch sử")){check(true,"Application context rejects edits to retained audit versions");}
        }
        var rollbackCode="ROLLBACK-"+Guid.NewGuid().ToString("N");
        await using(var db=new ApplicationDbContext(options)){await using var tx=await db.BeginBusinessWriteAsync();db.InputLots.Add(new(){Code=rollbackCode});await db.SaveChangesAsync();}
        await using(var db=new ApplicationDbContext(options))check(!await db.InputLots.AnyAsync(l=>l.Code==rollbackCode) && !await db.CultivationAudits.AnyAsync(a=>a.AfterJson!=null && a.AfterJson.Contains(rollbackCode)),"Business rollback also rolls back its audit trail atomically");
        var policy=WebUtility.HtmlDecode(await client.GetStringAsync("/RecordHistory"));check(policy.Contains("24 tháng") && policy.Contains("không có chức năng xóa vĩnh viễn"),"Retention policy is visible and does not purge archived records");
        check((await client.GetAsync("/HarvestTrace?code="+code)).StatusCode==HttpStatusCode.OK,"Harvest batch can be looked up by its code");
        await EvidenceChecks();

        async Task EvidenceChecks()
        {
            var fields=Inspection("FieldResource",soilId,"Pass","2024-01-01");
            var html=await client.GetStringAsync($"/FarmRecords/CreateInspection?targetType=FieldResource&targetId={soilId}");TestChangeFields.Apply(fields,html);
            fields["__RequestVerificationToken"]=System.Net.WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            using var fake=new MultipartFormDataContent();foreach(var pair in fields)fake.Add(new StringContent(pair.Value),pair.Key);fake.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<svg onload='x()'>")),"Evidence","fake.pdf");
            check((await client.PostAsync("/FarmRecords/CreateInspection",fake)).StatusCode==HttpStatusCode.OK,"Inspection upload rejects disguised executable content");
            using var valid=new MultipartFormDataContent();foreach(var pair in fields)valid.Add(new StringContent(pair.Value),pair.Key);valid.Add(new ByteArrayContent(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n% disposable test fixture\n%%EOF")),"Evidence","lab.pdf");
            check((await client.PostAsync("/FarmRecords/CreateInspection",valid)).StatusCode==HttpStatusCode.Redirect,"Inspection accepts a PDF evidence document");
            int reportId;string path;
            await using(var db=new ApplicationDbContext(options)){var report=await db.InspectionReports.SingleAsync(r=>r.ReportNumber==fields["ReportNumber"]);reportId=report.Id;path=report.EvidencePath!;}
            check(FarmRecordRules.EvidenceLocal(path) && (await client.GetAsync(path)).StatusCode==HttpStatusCode.OK,"Lab evidence uses an application-generated local path");
            var updated=Inspection("FieldResource",soilId,"Pass","2024-01-01");updated["Id"]=reportId.ToString();updated["ReportNumber"]=fields["ReportNumber"];updated["Result"]="Đã đối chiếu lại nội dung phiếu";
            check((await Post($"/FarmRecords/EditInspection/{reportId}",updated)).StatusCode==HttpStatusCode.Redirect,"Inspection correction retains its existing evidence file");
            await using(var db=new ApplicationDbContext(options))check((await db.InspectionReports.FindAsync(reportId))!.EvidencePath==path && await db.CultivationAudits.AnyAsync(a=>a.RecordType=="InspectionReport" && a.RecordId==reportId && a.BeforeJson!=null && a.BeforeJson.Contains(path)),"Inspection history retains old evidence references");
            var root=new DirectoryInfo(AppContext.BaseDirectory);while(root!=null && !File.Exists(Path.Combine(root.FullName,"Farm-Management.sln")))root=root.Parent;
            File.Delete(Path.Combine(root!.FullName,"Farm-Management","wwwroot","uploads","evidence",Path.GetFileName(path)));
            await using(var db=new ApplicationDbContext(options)){var report=await db.InspectionReports.FindAsync(reportId);report!.EvidencePath=null;await db.SaveChangesAsync();}
        }

        async Task<HttpResponseMessage> Post(string route,Dictionary<string,string> fields,string? source=null)
        {
            var html=await client.GetStringAsync(source??route);TestChangeFields.Apply(fields,html);
            fields["__RequestVerificationToken"]=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            if(fields["__RequestVerificationToken"].Length==0)throw new Exception("Missing token for "+route);
            return await client.PostAsync(route,new FormUrlEncodedContent(fields));
        }
    }
}
