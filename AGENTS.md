# Quy tắc phát triển dự án Farm Management

## Thiết kế database mềm

- Database ưu tiên lưu trữ linh hoạt; các ràng buộc nghiệp vụ được thực thi ở giao diện và backend. Đây là quy ước mặc định cho model, cấu hình Entity Framework Core và migration mới hoặc được chỉnh sửa.
- Các cột dữ liệu nghiệp vụ mặc định cho phép `NULL`, kể cả các cột tham chiếu đến bản ghi khác. Trong C#, dùng kiểu nullable tương ứng (`string?`, `int?`, `decimal?`, `DateTime?`, ...). Chỉ giữ các ràng buộc kỹ thuật cần thiết cho định danh bản ghi, như khóa chính và cơ chế sinh ID; khóa chính không nullable.
- Dùng khóa ngoại mềm: lưu ID tham chiếu bằng cột nullable nhưng không tạo `FOREIGN KEY` constraint trong database. Không bật cascade delete hoặc cascade update ở database.
- Không tạo ràng buộc nghiệp vụ bằng `NOT NULL`, `UNIQUE`, `CHECK` hoặc trigger ở database. Ví dụ: mã công nhân không trùng, một công nhân chỉ có một lần chấm công mỗi ngày, số giờ hợp lệ và số tiền dương phải được kiểm tra ở backend. Có thể tạo index không unique để hỗ trợ truy vấn.
- Không dùng `[Required]`, `.IsRequired()` hoặc cấu hình quan hệ EF Core trên entity lưu trữ theo cách khiến migration sinh cột `NOT NULL` hay constraint khóa ngoại. Ưu tiên ViewModel/DTO riêng cho form và API để khai báo validation nghiệp vụ. Chỉ dùng navigation property khi đã bảo đảm mapping và migration không tự sinh khóa ngoại cứng; nếu cần, bỏ mapping navigation và truy vấn/join tường minh bằng ID.

## Kiểm tra tại giao diện và backend

- Giao diện phải kiểm tra các trường bắt buộc, định dạng, miền giá trị và lựa chọn bản ghi liên quan theo từng nghiệp vụ, đồng thời hiển thị thông báo lỗi rõ ràng.
- Backend phải kiểm tra lại mọi dữ liệu đầu vào trước khi ghi, kể cả request AJAX/API; không tin cậy validation ở trình duyệt. Kiểm tra sự tồn tại và trạng thái hợp lệ của ID tham chiếu khi nghiệp vụ yêu cầu.
- Kiểm tra trùng dữ liệu và quy tắc thêm/sửa/xóa tại backend. Với thao tác có thể xảy ra đồng thời, dùng transaction và cơ chế điều phối phù hợp để giữ đúng quy tắc nghiệp vụ dù database không có unique/FK constraint.
- Khi xóa bản ghi có dữ liệu liên quan, backend phải xử lý theo quy tắc nghiệp vụ: từ chối xóa, chuyển trạng thái ngừng sử dụng, hoặc cập nhật tham chiếu về `NULL`. Không dựa vào database để chặn xóa hay tự xóa dữ liệu liên quan.
- Mọi truy vấn, tính toán và giao diện phải xử lý được dữ liệu `NULL` hoặc tham chiếu không còn tồn tại, tránh lỗi null và không tự coi dữ liệu thiếu là dữ liệu hợp lệ. Không tự đổi dữ liệu thiếu thành `0`, chuỗi rỗng hay ngày hiện tại nếu nghiệp vụ chưa quy định.

## Migration và phạm vi thay đổi

- Trước khi chấp nhận migration, kiểm tra mã migration và model snapshot để bảo đảm các cột nghiệp vụ nullable, không có foreign key constraint, unique constraint/index, check constraint hoặc cascade ngoài ý muốn.
- Khi được yêu cầu chuyển schema hiện có sang quy ước này, tạo migration mới để bỏ ràng buộc và đổi nullable, giữ nguyên dữ liệu hiện có; không sửa migration đã áp dụng để thay thế việc nâng cấp schema.
- Việc thêm quy tắc vào file này không đồng nghĩa với yêu cầu tự động sửa toàn bộ model, tạo migration hay cập nhật database đang chạy. Chỉ thực hiện chuyển đổi schema khi nằm trong phạm vi công việc người dùng yêu cầu.
