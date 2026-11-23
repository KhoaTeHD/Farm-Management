# Thiên Mỹ Farm — Windows Server / IIS

Domain: **https://quanly.thienmyfarm.com**. Gói này chứa thư mục `app/` đã biên dịch, `web.config`, SQL migration và lệnh khởi tạo quản trị. Không chứa mật khẩu, tài khoản mẫu hoặc tệp người dùng. Không cần cài SDK trên VPS.

## Cài nhanh bằng BAT (khuyến nghị)

1. Trỏ DNS **A `quanly` → IP public của VPS IIS**; bỏ AAAA sai nếu có. Mở 80/443 cả ở nhà cung cấp VPS. Tạm tắt proxy Cloudflare trong lúc cấp chứng chỉ.
2. Giải nén toàn bộ ZIP trên Windows Server x64. Nhấp phải **`DEPLOY-QUANLY.bat` → Run as administrator**. Bộ cài thêm IIS, tải Hosting Bundle .NET 8 nếu thiếu (kiểm checksum và chữ ký Microsoft), tạo pool/site ThienMyFarm tại `C:\Sites\ThienMyFarm`, cấp quyền, mở firewall Windows 80/443 và triển khai ứng dụng. Nếu báo yêu cầu restart, khởi động lại Windows rồi chạy lại BAT.
3. Khi được hỏi, nhập chuỗi kết nối SQL (được che). Nếu đã có `appsettings.Local.json`, bộ cài giữ nguyên. Dùng database đã cập nhật của dự án; nếu dùng database mới, làm bước SQL ở mục 2 bên dưới trước khi chạy. BAT không xóa/tạo lại hoặc tự thay schema database.
4. Bộ cài tự kiểm tra database: nếu đã có quản trị hoạt động thì giữ nguyên tài khoản và chuyển sang SSL; chỉ hỏi tài khoản/mật khẩu khi database chưa có tài khoản. Mật khẩu quá ngắn sẽ được yêu cầu nhập lại ngay. Nhập email ACME của bạn, xác nhận DNS/cổng và điều khoản Let's Encrypt khi được hỏi.
5. Bộ cài tải **win-acme** từ bản phát hành chính thức đã ghim checksum, cấp chứng chỉ Let's Encrypt, gắn HTTPS 443/SNI, kiểm tra tác vụ gia hạn và thử mở trang đăng nhập HTTPS. Công cụ gia hạn nằm lâu dài trong `C:\ProgramData\ThienMyFarm\win-acme\`; không xóa thư mục đó hoặc `%ProgramData%\win-acme`.
6. Nếu chỉ SSL lỗi (DNS/cổng chưa đúng), sửa cấu hình rồi chạy **`INSTALL-SSL.bat` → Run as administrator**, không cần chép lại ứng dụng. SSL chỉ thực sự được cấp khi chạy trên VPS và CA xác minh được domain; file ZIP không chứa chứng chỉ đã cấp.

Chạy lại bộ cài tạo bản sao tệp ở `C:\ProgramData\ThienMyFarm\backups\`, giữ cấu hình riêng và uploads, không dùng `/MIR`; chỉ dừng site/pool ThienMyFarm. Vẫn cần sao lưu SQL riêng. Nếu có site khác dùng domain hoặc pool này, bộ cài dừng để tránh sửa nhầm. Nếu chép file thất bại sau khi dừng site, giữ site dừng, khôi phục từ backup hoặc sửa lỗi rồi chạy lại; không tự tuyên bố rollback thành công. Không tự `iisreset` toàn máy.

Kiểm tra gói trên máy lập trình mà không đổi IIS/firewall/SSL:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Iis.ps1 -CheckPackage
```

Giữ cổng 80 cho lần gia hạn HTTP-01. Theo dõi Task Scheduler và log `%ProgramData%\win-acme`; không liên tục yêu cầu cấp mới để tránh giới hạn CA. Dependency được ghim trong `deployment-dependencies.json`; cập nhật URL/checksum từ nhà phát hành trước khi cần nâng bản. [win-acme HTTP-01](https://www.win-acme.com/reference/plugins/validation/http/selfhosting), [gia hạn tự động](https://www.win-acme.com/manual/automatic-renewal).

Các mục bên dưới là hướng dẫn cấu hình thủ công và xử lý sự cố.

## 1. Chuẩn bị VPS

1. Trong Server Manager, cài **Web Server (IIS)** và IIS Management Console. Dùng Windows Server x64 còn được Microsoft hỗ trợ.
2. Cài **.NET 8 Hosting Bundle x64** bản vá mới nhất từ https://dotnet.microsoft.com/en-us/download/dotnet/8.0 (mục ASP.NET Core Runtime → Windows → Hosting Bundle). Cài sau IIS; nếu đã cài trước IIS thì chạy Repair. Khởi động lại VPS sau khi cài.
3. DNS: tạo bản ghi **A**, tên `quanly`, giá trị là **IP public của VPS chạy IIS** trong zone `thienmyfarm.com`. Không tự dùng IP SQL Server nếu đó là máy khác. Nếu có AAAA thì nó cũng phải trỏ đúng máy phục vụ. Mở TCP 80/443 ở firewall VPS và firewall nhà cung cấp.
4. Chuẩn bị chứng chỉ TLS hợp lệ cho `quanly.thienmyfarm.com` trong Windows Certificate Store (Local Computer → Personal), có private key. Có thể dùng công cụ ACME hỗ trợ IIS; cần cấu hình tự gia hạn. Nếu cài thủ công, tự cấp/gắn chứng chỉ; bộ cài BAT ở trên hỗ trợ cấp SSL nhưng không thay DNS.

## 2. Chép gói và kết nối SQL Server

1. Giải nén ZIP, chép **nội dung `app/`** vào `C:\Sites\ThienMyFarm` (đây là Physical Path của website, không phải thư mục ZIP ngoài).
2. Sao chép `appsettings.Local.example.json` thành **`C:\Sites\ThienMyFarm\appsettings.Local.json`**. Sửa `DefaultConnection` bằng kết nối SQL Server thực tế đã dùng. File nằm cạnh `Farm-Management.dll`, không nằm trong `wwwroot`. Dùng trình soạn thảo trên VPS; không gửi mật khẩu trong URL hay lệnh shell. Chuỗi JSON phải escape `\` thành `\\` nếu dùng tên instance.
3. SQL Server phải cho phép kết nối từ VPS đến cổng SQL thực tế. Ưu tiên tài khoản SQL riêng cho ứng dụng; tài khoản chạy migration cần quyền thay đổi schema. Với máy SQL chưa có chứng chỉ tin cậy, chỉ đặt `TrustServerCertificate=True` khi đã xác nhận đúng máy chủ; không tắt mã hóa tùy tiện.
4. Database đang dùng của dự án đã được cập nhật trong phiên triển khai tính năng. Nếu dùng chính database đó, không cần tạo database hay nạp lại dữ liệu. Nếu dùng database khác, tạo database `FarmManagement`, sao lưu trước rồi mở **`database-update.sql`** bằng SSMS, chọn đúng database và Execute. Script idempotent kiểm tra `__EFMigrationsHistory`, không dùng chế độ xóa/tạo lại database. Giữ lịch sử migration khi chuyển dữ liệu. Ứng dụng **không tự chạy migration lúc khởi động**.

## 3. Khởi tạo quản trị đầu tiên

Nếu dùng database đã có quản trị, bỏ qua bước này và đăng nhập bằng tài khoản đó. Lệnh chỉ tạo quản trị khi chưa có tài khoản; không ghi đè hay đặt lại mật khẩu đang tồn tại.

Mở PowerShell trong thư mục giải nén, chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Initialize-Admin.ps1 -AppDirectory C:\Sites\ThienMyFarm
```

Nhập tên tài khoản và mật khẩu tạm (12–128 ký tự, có chữ và số). Mật khẩu nhập được che, chỉ truyền cho tiến trình con; không lưu trong script/config. Lần đăng nhập đầu bắt buộc đổi mật khẩu. Sau đó vào **Quản lý tài khoản → Thêm tài khoản** để cấp riêng từng người. Quản trị có mọi quyền và quản lý tài khoản; nhân viên dùng nghiệp vụ trang trại, chấm công/lương/tạm ứng, không quản lý tài khoản. Khóa tài khoản thay vì xóa để giữ hồ sơ.

## 4. Tạo website IIS và quyền thư mục

1. IIS Manager → Application Pools → Add: tên **ThienMyFarm**, `.NET CLR Version = No Managed Code`, Integrated. Advanced Settings: **Enable 32-Bit Applications = False**, **Load User Profile = True**, Identity = ApplicationPoolIdentity. Giữ profile của pool để lưu khóa bảo vệ cookie qua các lần recycle.
2. Sites → Add Website: tên **ThienMyFarm**, pool **ThienMyFarm**, Physical Path `C:\Sites\ThienMyFarm`. Binding HTTP: port 80, host name `quanly.thienmyfarm.com`.
3. Site → Bindings → Add HTTPS: port 443, host name `quanly.thienmyfarm.com`, **Require Server Name Indication**, chọn đúng chứng chỉ. HTTPS phải hoạt động vì cookie đăng nhập ở Production chỉ gửi qua HTTPS. Ứng dụng tự chuyển HTTP sang HTTPS.
4. Site → Authentication: bật Anonymous Authentication (đăng nhập do ứng dụng xử lý). Anonymous → Edit → **Application pool identity**. Không bật Basic Authentication nếu không chủ ý dùng thêm lớp IIS.
5. PowerShell Administrator, cấp quyền tối thiểu:

```powershell
New-Item -ItemType Directory -Force C:\Sites\ThienMyFarm\wwwroot\uploads | Out-Null
icacls C:\Sites\ThienMyFarm /grant 'IIS AppPool\ThienMyFarm:(OI)(CI)(RX)'
icacls C:\Sites\ThienMyFarm\wwwroot\uploads /grant 'IIS AppPool\ThienMyFarm:(OI)(CI)(M)'
```

Không cấp Everyone Full Control hoặc quyền ghi toàn bộ site. File kết nối chỉ quản trị VPS được sửa, pool được đọc. Ứng dụng chạy môi trường Production mặc định; bỏ biến `ASPNETCORE_ENVIRONMENT=Development` nếu có ở máy/site/pool.

## 5. Kiểm tra sau deploy

- Truy cập HTTPS domain: chuyển tới đăng nhập, tài khoản tạm phải đổi mật khẩu. Sai 5 lần sẽ khóa đăng nhập tài khoản 15 phút; giới hạn thêm 20 request đăng nhập/đổi mật khẩu mỗi phút theo IP.
- Quản trị mở được Quản lý tài khoản; nhân viên bị từ chối khi gõ trực tiếp `/Accounts`, kể cả request POST. Thử khóa tài khoản đang đăng nhập ở trình duyệt khác: request tiếp theo phải yêu cầu đăng nhập lại.
- Khi chưa đăng nhập, trang lương/mùa vụ/truy xuất và đường dẫn `/uploads/...` đều phải yêu cầu đăng nhập. Tài nguyên CSS/JS vẫn tải được.
- Thử tạo một ghi chép của chính bạn, đính kèm ảnh/phiếu rồi kiểm tra lịch sử ghi đúng tài khoản. Nhật ký chăm sóc vẫn giữ trường người thực hiện riêng.
- Đăng xuất, đăng nhập lại; recycle pool rồi đăng nhập lại để kiểm tra lưu khóa cookie. Kiểm tra chức năng in, truy xuất và ảnh.

## 6. Nâng cấp, sao lưu và khắc phục lỗi

Sao lưu **SQL Server + `wwwroot/uploads/` + `appsettings.Local.json`** trước khi nâng cấp. Giữ khóa Data Protection/profile pool; không dùng chung cookie keys với ứng dụng khác. Hồ sơ không có chức năng tự xóa vĩnh viễn. Không đưa backup, log hoặc script SQL vào Physical Path của site.

Khi cập nhật: dừng riêng site/pool ThienMyFarm, sao lưu, chép đè file publish; **giữ `appsettings.Local.json` và toàn bộ `wwwroot/uploads/`**. Không dùng đồng bộ kiểu xóa mọi file thừa (`/MIR`), không xóa thư mục site. Chạy migration nếu có, rồi khởi động lại pool/site. Không ghi đè file cấu hình riêng bằng file ví dụ. Quản trị đã tồn tại không chạy khởi tạo lần nữa.

- **500.19 / 500.21**: kiểm tra Hosting Bundle, AspNetCoreModuleV2 và `web.config` đúng bản publish.
- **500.30 / 500.31**: kiểm tra .NET 8 x64 runtime, quyền đọc file, Event Viewer → Application → IIS AspNetCore Module V2 và kết nối SQL. Không bật trang lỗi Development trên domain.
- **Lặp chuyển hướng / đăng nhập xong lại về đăng nhập**: kiểm tra HTTPS binding, chứng chỉ, môi trường Production. Nếu dùng Cloudflare/proxy, dùng TLS đầy đủ tới IIS; không dùng Flexible HTTP tới origin.
- **Lỗi lưu ảnh/phiếu**: kiểm tra quyền Modify chỉ trên `wwwroot/uploads` của đúng pool.
- **Quên mật khẩu**: nhờ một quản trị khác cấp lại ở trang tài khoản; không tạo cổng reset công khai. Giữ ít nhất hai quản trị được tin cậy để có thể hỗ trợ nhau. Lệnh khởi tạo không phải cửa sau khôi phục tài khoản.

Hướng dẫn Microsoft: [Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-8.0), [Cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-8.0), [Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-8.0).
