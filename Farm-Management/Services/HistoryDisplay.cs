using System.Globalization;
using System.Text.Json;

namespace Farm_Management.Services;

public static class HistoryDisplay
{
    public static string Label(string key)=>Labels.GetValueOrDefault(key,key);
    public static string Value(string key,JsonElement? element)
    {
        if(element==null || element.Value.ValueKind==JsonValueKind.Null)return "Chưa có";
        var text=element.Value.ValueKind==JsonValueKind.String?element.Value.GetString()??"":element.Value.ToString();
        if(element.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)return element.Value.GetBoolean()?"Có":"Không";
        if((key.EndsWith("Date") || key=="Date") && DateTime.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var day))return day.ToString("dd/MM/yyyy");
        if(key=="DeletedAt" && DateTime.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var time))return FarmRecordRules.Time(time);
        if(key=="Status")return FarmRecordRules.Statuses.GetValueOrDefault(text,CropRules.Label(text));
        if(key=="Kind")return FarmRecordRules.InputKinds.GetValueOrDefault(text,FarmRecordRules.ResourceKinds.GetValueOrDefault(text,CropSeasonRules.KindLabel(text)));
        if(key=="GrowthStage")return CropSeasonRules.Stages.GetValueOrDefault(text,"Chưa rõ giai đoạn");
        if(key=="Condition")return CropSeasonRules.Conditions.GetValueOrDefault(text,"Chưa rõ tình trạng");
        if(key=="Conclusion")return FarmRecordRules.Conclusions.GetValueOrDefault(text,"Chưa rõ kết luận");
        if(key=="TargetType")return FarmRecordRules.RecordLabel(text);
        return text.Length>700?text[..700]+"…":text;
    }
    private static readonly Dictionary<string,string> Labels=new()
    {
        ["AreaId"]="Vùng trồng",["PlantId"]="Mùa vụ",["PlantTypeId"]="Loại cây",["SeedId"]="Giống",["CareStepId"]="Việc dự kiến",
        ["Summary"]="Nội dung ghi chép",["Date"]="Ngày thực hiện",["Recorder"]="Người thực hiện",["Amount"]="Lượng dùng / thu hoạch",["Unit"]="Đơn vị",["Material"]="Sản phẩm",
        ["HeightCm"]="Chiều cao (cm)",["SurvivalPercent"]="Tỷ lệ sống (%)",["AffectedPercent"]="Tỷ lệ ảnh hưởng (%)",["GrowthStage"]="Giai đoạn cây",["Condition"]="Tình trạng cây",
        ["IsDeleted"]="Đã lưu trữ",["DeletedAt"]="Thời điểm lưu trữ",["Revision"]="Phiên bản",["InputLotId"]="Lô vật tư",["WaterResourceId"]="Nguồn nước",["PhotoPath"]="Ảnh",["EvidencePath"]="Phiếu đính kèm",
        ["Status"]="Trạng thái",["SeasonName"]="Tên mùa vụ",["CareProtocol"]="Quy trình",["SeasonReview"]="Kinh nghiệm",["NextSeasonChanges"]="Điều chỉnh mùa sau",["Result"]="Kết quả",["Conclusion"]="Kết luận",
        ["Quantity"]="Số lượng",["Code"]="Mã hồ sơ",["BatchCode"]="Mã lô trồng",["ProductName"]="Tên sản phẩm",["Supplier"]="Nhà cung cấp",["HarvestDate"]="Ngày thu hoạch",["WeightKg"]="Sản lượng (kg)",["Quality"]="Chất lượng",
        ["Kind"]="Nhóm / công việc",["ManufacturerBatch"]="Số lô nhà sản xuất",["ReceivedDate"]="Ngày nhận",["ExpiryDate"]="Hạn dùng",["RegistrationNumber"]="Số đăng ký",["InvoiceNumber"]="Số hóa đơn",["Name"]="Tên",["Location"]="Vị trí",
        ["RiskAssessment"]="Nguy cơ đã kiểm tra",["Treatment"]="Cách xử lý",["TargetType"]="Loại hồ sơ lấy mẫu",["TargetId"]="Hồ sơ lấy mẫu",["ReportNumber"]="Số phiếu",["Laboratory"]="Đơn vị kiểm nghiệm",["SampleDate"]="Ngày lấy mẫu",["ReportDate"]="Ngày trả kết quả",["Indicator"]="Chỉ tiêu",["LimitReference"]="Ngưỡng / tài liệu đối chiếu",
        ["Origin"]="Nguồn gốc",["BatchNumber"]="Số lô giống",["IsCertified"]="Có chứng nhận theo hồ sơ",["Description"]="Mô tả",["GrowthCycleDays"]="Chu kỳ (ngày)",["Acreage"]="Diện tích (m²)",["GpsCoordinates"]="Vị trí bản đồ",["SoilType"]="Loại đất",["SoilTestResult"]="Kết quả đất ghi trước đây",["SoilTestDate"]="Ngày kiểm tra đất",
        ["Notes"]="Ghi chú",["Title"]="Tên việc",["PlannedDay"]="Ngày thứ mấy sau trồng",["Instructions"]="Cách làm dự kiến",["Method"]="Cách thực hiện",["Weather"]="Thời tiết",["WithdrawalDays"]="Ngày cách ly",["IsVoided"]="Mã đã ngừng dùng",["PlantingDate"]="Ngày trồng",["ExpectedHarvestDate"]="Thu hoạch dự kiến",["ExpectedSafeHarvestDate"]="Mốc cách ly cũ",["JournalEntryId"]="Ghi chép thu hoạch",["LegacyHarvestId"]="Đợt thu hoạch sổ cũ",["SnapshotJson"]="Bản hồ sơ truy xuất"
    };
}
