# Kiểm thử Farm Management

Chạy tại thư mục gốc repository:

```powershell
dotnet build tests/Farm-Management.Checks -c Release
dotnet tests/Farm-Management.Checks/bin/Release/net8.0/Farm-Management.Checks.dll --integration
```

Bộ kiểm thử dùng SQL Server LocalDB mặc định và tự tạo, xóa database riêng có tiền tố `FarmManagement_Checks_`. Không dùng database ứng dụng. Máy cần có .NET 8 và LocalDB; có thể đặt biến `FARM_TEST_SQLSERVER` để dùng SQL Server kiểm thử hỗ trợ tạo database.

Các kiểm tra gồm schema nullable/khóa ngoại mềm, nâng cấp dữ liệu cũ, giữ nguyên snapshot lương trước tính năng tạm ứng, validation DTO, các form MVC, chống CSRF, request đồng thời, chấm công, chốt/mở lại bảng lương, tạm ứng và khấu trừ, CSV, tham chiếu thiếu, loại cây/giống/lô trồng và thời gian cách ly thuốc. Log `PASS` nêu rõ từng điều kiện đã kiểm tra. Chạy không có `--integration` chỉ kiểm tra model và tính toán trong bộ nhớ.

`--preview` chạy toàn bộ kiểm thử, sau đó giữ ứng dụng và database kiểm thử để kiểm tra giao diện. URL được ghi ở `.local/qa-preview.json`; tạo file `.local/stop-qa` để dừng ứng dụng và xóa database kiểm thử.

Tạm ứng được khấu trừ vào đúng tháng đã chọn, không phụ thuộc tháng của ngày ứng. Thực lĩnh bằng lương gộp trừ tổng phiếu ứng; ứng vượt lương chặn chốt và yêu cầu điều chỉnh tháng, không tự chuyển nợ. Phiếu thuộc tháng đã chốt không được thêm/sửa/xóa; mở lại cho phép điều chỉnh nhưng giữ snapshot cũ.

Mỗi lô trồng là một mùa vụ riêng. Có kế hoạch theo ngày sau trồng, form ghi tưới/bón phân/dùng thuốc/chăm sóc, quan sát giai đoạn và chỉ số sinh trưởng, ảnh JPG/PNG/WebP, từng đợt thu hoạch, kinh nghiệm và điều chỉnh mùa sau. Sổ cũ vẫn xuất hiện trong dòng thời gian. So sánh hai mùa vụ trình bày kết quả ghi nhận, không suy diễn nguyên nhân hoặc tự kê quy trình. Vụ mới có thể lấy các việc dự kiến hợp lệ cùng loại cây, không sao chép kết quả hay nhật ký thực tế.

Kiểm tra mùa vụ còn gồm chống nhập trùng khi gửi đồng thời, ngày trước trồng/tương lai, chỉ số sinh trưởng, đơn vị lượng dùng, kế hoạch liên kết sai lô, bảo toàn kế hoạch đã thực hiện, chặn thuốc ghi lùi ngày làm mất hợp lệ một đợt thu hoạch, upload ảnh và hướng dẫn nhập liệu tại tổng quan. Ảnh là dữ liệu runtime trong `wwwroot/uploads/crops/`, không đưa vào Git; khi sao lưu cần giữ cả database và thư mục ảnh. Tệp ảnh gỡ khỏi ghi chép được giữ trong kho để tránh xóa nhầm ảnh đang cần khôi phục.


Lịch sử canh tác được lưu tự động cùng transaction cho vùng/loại cây/giống/mùa vụ/kế hoạch/nhật ký/vật tư/nguồn/kiểm nghiệm/truy xuất. Sửa và lưu trữ cần tên tự khai, lý do và phiên bản hồ sơ; request cũ bị trả 409 để tránh ghi đè. Xóa qua ứng dụng chuyển sang lưu trữ, có thể khôi phục khi tham chiếu và nghiệp vụ còn hợp lệ. Không có purge hoặc lịch tự xóa: giữ toàn bộ lâu hơn mức tối thiểu 24 tháng từ thu hoạch. Migration chụp baseline hồ sơ cũ, không suy diễn lịch sử hoặc danh tính trước đó.

Vật tư mới có lô nhà sản xuất, nhà cung cấp, hóa đơn, ngày/hạn, lượng nhận và trạng thái kiểm tra. Nhật ký bón/phun liên kết lô vật tư, tưới liên kết nguồn của đúng vùng. Chặn dùng quá lượng, sai đơn vị, trước ngày nhận/sau hạn hoặc nguồn/vật tư đang tạm giữ. Kiểm nghiệm lưu chỉ tiêu, kết quả, tài liệu/ngưỡng, kết luận và PDF/ảnh; kết quả chưa đạt/chưa rõ tạm giữ đối tượng, kết quả khắc phục được thêm riêng cùng chỉ tiêu. Việc ghi kết luận vẫn cần người phụ trách đối chiếu phiếu và tài liệu phù hợp.

Thu hoạch mới được cấp mã tự động với snapshot nguồn gốc và nhật ký. Sửa sổ gốc không ghi đè snapshot; cập nhật có lý do giữ cùng mã và giữ bản trước trong lịch sử. Lưu trữ thu hoạch hoặc kiểm nghiệm sản phẩm không đạt sẽ ngừng dùng mã. Truy xuất hiện phục vụ quản lý trong ứng dụng, chưa là cổng công bố/chứng nhận công khai. Cân bằng khối lượng đầy đủ, kiểm tra nội bộ và chứng nhận VietGAP không nằm trong phạm vi hoàn tất của đợt này.

Lịch sử bắt đầu khi tính năng được triển khai. Tên người nhập chưa có xác thực tài khoản; người có quyền SQL trực tiếp vẫn có thể vượt qua cơ chế ứng dụng. Đây không phải nhật ký chống sửa ở cấp quản trị database. Tệp phiếu mới nằm trong `wwwroot/uploads/evidence/`; sao lưu cùng `wwwroot/uploads/crops/` và database. Không xóa tệp cũ khi sửa/lưu trữ hồ sơ.


Hồ sơ mùa vụ và bản truy xuất có bảng Thuốc và phân bón đã dùng: từng lần dùng hiển thị tên sản phẩm, ngày, lượng/đơn vị, người thực hiện và số ngày cách ly. Không tự thay dữ liệu thiếu bằng 0 hoặc lấy tên lô nhập hiện tại để giả định tên đã ghi lúc sử dụng. Sổ cũ đối chiếu tên thuốc/phân và công nhân bằng ID mềm, báo thiếu khi không tìm được. Chi tiết sổ cũ được lưu vào snapshot mới; snapshot cũ thiếu phần này vẫn đọc được và có thông báo cần đối chiếu/cập nhật, không tự dựng lại lịch sử. Khi in, bảng sử dụng hiện toàn bộ các dòng dù giao diện đang lọc/phân trang.
