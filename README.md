# Service Booking Management System

Hệ thống quản lý đặt lịch dịch vụ (salon/spa) — bài kiểm tra dự án demo Full-stack Intern.

- **Backend**: ASP.NET Core 8 Web API, Entity Framework Core, SQL Server
- **Frontend**: Next.js 14 (App Router), TypeScript, TailwindCSS
- **Auth**: JWT, phân quyền Customer / Admin
- **Realtime**: SignalR — cập nhật booking tức thời, không cần F5
- **Background job**: Hangfire — tự động xử lý booking quá hạn

## Mục lục

- [Yêu cầu môi trường](#yêu-cầu-môi-trường)
- [Cách chạy](#cách-chạy)
- [Tài khoản demo](#tài-khoản-demo)
- [Unit test](#unit-test)
- [Postman Collection / Swagger](#postman-collection--swagger)
- [SignalR — cập nhật booking real-time](#signalr--cập-nhật-booking-real-time)
- [Hangfire — tự động xử lý booking quá hạn](#hangfire--tự-động-xử-lý-booking-quá-hạn)
- [Danh sách chức năng đã hoàn thành](#danh-sách-chức-năng-đã-hoàn-thành)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Push code lên GitHub](#push-code-lên-github)

---

## Yêu cầu môi trường

| Công cụ | Version tối thiểu | Dùng cho |
|---|---|---|
| .NET SDK | 8.0 | Backend |
| Node.js | 18.18+ (khuyến nghị 20 LTS) | Frontend |
| SQL Server | 2019+ (LocalDB / Express đều được) | Database + Hangfire storage |
| `dotnet-ef` tool | 8.x | Chạy migration |

---

## Cách chạy

### 1. Database

Sửa connection string trong `backend/ServiceBooking.API/appsettings.json` (`ConnectionStrings:DefaultConnection`) cho đúng SQL Server của bạn. Ví dụ dùng LocalDB:

```
Server=(localdb)\mssqllocaldb;Database=ServiceBookingDb;Trusted_Connection=True;TrustServerCertificate=True;
```

Tạo schema + seed data — **chọn 1 trong 2 cách**:

**Cách A — EF Core Migration (khuyến nghị):**

```bash
cd backend/ServiceBooking.API
dotnet tool install --global dotnet-ef --version 8.*   # nếu chưa có
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Cách B — SQL script có sẵn** (không cần cài `dotnet-ef`):

Chạy file `database/schema_and_seed.sql` trực tiếp trên SQL Server (SSMS / Azure Data Studio / `sqlcmd`).

> Hangfire tự tạo thêm các bảng riêng (tiền tố `HangFire.*`) trong cùng database này ngay lần chạy đầu tiên — không cần tạo tay.

### 2. Backend

```bash
cd backend/ServiceBooking.API
dotnet restore
dotnet run
```

- Swagger UI: `http://localhost:{port}/swagger` (port hiện ở terminal sau khi `dotnet run`)
- Hangfire Dashboard: `http://localhost:{port}/hangfire` (chỉ bật khi chạy ở môi trường Development — xem giải thích ở mục Hangfire bên dưới)

### 3. Frontend

```bash
cd frontend
npm install
cp .env.local.example .env.local
```

Mở `.env.local`, chỉnh `NEXT_PUBLIC_API_URL` đúng port backend đang chạy, ví dụ:

```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

```bash
npm run dev
```

Mở `http://localhost:3000`.

> Đảm bảo `Cors:AllowedOrigins` trong `appsettings.json` backend có chứa `http://localhost:3000` (mặc định đã có sẵn).

---

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@bookingdemo.com | Admin@123 |
| Customer | customer1@bookingdemo.com | Customer@123 |
| Customer | customer2@bookingdemo.com | Customer@123 |

Dữ liệu mẫu đi kèm: 2 nhân viên, 5 dịch vụ, lịch làm việc 7 ngày, 10 booking ở đủ 4 trạng thái (xem chi tiết trong `backend/ServiceBooking.API/Data/SeedData.cs`).

---

## Unit test

Test tập trung vào phần nghiệp vụ quan trọng nhất: **chống trùng lịch** và **validation** (`BookingService`, `StaffManagementService`, `ServiceManagementService`), dùng SQLite in-memory (mô phỏng transaction thật, không dùng EF InMemory provider vì provider đó không hỗ trợ transaction).

```bash
cd backend/ServiceBooking.Tests
dotnet test
```

Các nhóm test chính (đối chiếu trực tiếp với TC1–TC6 trong đề bài):

- `CreateAsync_ThrowsBadRequest_WhenStartTimeInPast` — TC1
- `CreateAsync_ThrowsBadRequest_WhenOutsideWorkingHours` — TC2
- `CreateAsync_Throws409Conflict_WhenOverlappingExistingBooking` + test biên (`NewStart == ExistingEnd` không tính là trùng) — TC3
- `CancelAsync_ThrowsForbidden_WhenActorIsNotOwnerAndNotAdmin` — TC4
- `CancelAsync_ThrowsBadRequest_WhenBookingAlreadyCompleted` — TC6
- `StaffManagementServiceTests` — chống trùng ca làm việc (mục 3 đề bài)
- `ServiceManagementServiceTests` — validate Duration > 0, Price >= 0

> TC5 (Customer không tự xác nhận/hoàn thành được) được enforce ở tầng `[Authorize(Roles = "Admin")]` trên Controller, không nằm trong logic của Service class nên không xuất hiện trong unit test này — đã verify riêng bằng Swagger (`02_Huong_Dan_Kiem_Thu_Swagger.md`) và qua UI (`03_Kiem_Thu_UI_Ngay3_Ngay4.md`).

Hướng dẫn chạy chi tiết hơn (đọc kết quả, chạy 1 test riêng lẻ, debug qua VS Code, lỗi thường gặp): xem `04_Huong_Dan_Ban_Giao.md`.

---

## Postman Collection / Swagger

- **Postman**: import file `postman/ServiceBookingSystem.postman_collection.json`. Chạy request "Login - Admin" và "Login - Customer" trong folder Auth trước — token sẽ tự động lưu vào collection variables (`adminToken`, `customerToken`), các request còn lại tự dùng đúng token. Hướng dẫn dùng chi tiết: `04_Huong_Dan_Ban_Giao.md`.
- **Swagger JSON**: khi backend đang chạy, xuất trực tiếp tại `https://localhost:{port}/swagger/v1/swagger.json` (mở URL này trên trình duyệt và Save As, hoặc `curl https://localhost:{port}/swagger/v1/swagger.json -o swagger.json`).

---

## SignalR — cập nhật booking real-time

**Vấn đề giải quyết**: trước đây, nếu Admin xác nhận 1 booking, Customer đang mở sẵn trang `/my-bookings` phải tự bấm F5 mới thấy trạng thái mới. Tương tự, Admin đang mở `/admin/bookings` không biết có booking mới vừa được đặt trừ khi tự tải lại trang.

**Cách hoạt động**:

- Backend có 1 Hub duy nhất tại `/hubs/bookings` (`Hubs/BookingHub.cs`). Khi client kết nối, Hub tự xếp họ vào group riêng: mọi user vào group `Customer-{userId}` của chính mình, Admin thêm vào group `Admins`.
- Sau mỗi lần `BookingService` tạo/đổi trạng thái/hủy 1 booking thành công, hệ thống bắn sự kiện `"BookingChanged"` tới đúng 2 group liên quan (`Admins` + `Customer-{customerId}` của booking đó) — xem `Services/BookingNotifier.cs`.
- Frontend (`lib/signalr.ts`) mở 1 kết nối SignalR dùng chung cho toàn app, gắn JWT qua `accessTokenFactory` (vì WebSocket của trình duyệt không cho đính custom header, nên token được gửi qua query string `access_token` — backend có xử lý riêng cho path `/hubs/*` trong `Program.cs`). Trang `/my-bookings` và `/admin/bookings` lắng nghe sự kiện này, tự gọi lại API tải danh sách khi có thay đổi.

**Cách quan sát thực tế**: mở 2 cửa sổ trình duyệt (1 đăng nhập Customer ở `/my-bookings`, 1 đăng nhập Admin ở `/admin/bookings`). Ở cửa sổ Admin, bấm "Xác nhận" 1 booking của Customer đó → cửa sổ Customer tự động cập nhật badge trạng thái ngay lập tức, **không cần F5**.

---

## Hangfire — tự động xử lý booking quá hạn

**Vấn đề giải quyết**: nếu Admin quên xác nhận 1 booking và giờ hẹn đã trôi qua, booking đó mãi kẹt ở trạng thái `Pending` dù thực tế không còn ý nghĩa. Tương tự, nếu Admin quên bấm "Hoàn thành" sau khi giờ hẹn đã kết thúc, booking mãi kẹt ở `Confirmed`.

**Cách hoạt động** (`Services/OverdueBookingProcessor.cs`, đăng ký chạy định kỳ mỗi 5 phút trong `Program.cs`):

1. Booking đang `Pending` mà `StartTime` đã qua → tự động chuyển `Cancelled` kèm lý do `"Hệ thống tự động hủy: quá giờ hẹn mà chưa được xác nhận."`
2. Booking đang `Confirmed` mà `EndTime` đã qua → tự động chuyển `Completed`

Mỗi booking bị xử lý đều bắn kèm sự kiện SignalR như trên, nên nếu Admin/Customer đang mở sẵn trang, họ thấy thay đổi ngay cả khi không phải chính họ bấm nút.

**Cách quan sát thực tế**:

- Xem Dashboard tại `https://localhost:{port}/hangfire` (chỉ khả dụng khi chạy Development — xem lưu ý bảo mật bên dưới) → tab **Recurring Jobs** thấy job `process-overdue-bookings`, bấm **Trigger now** để chạy thử ngay thay vì đợi 5 phút.
- Hoặc: tạo 1 booking cho vài phút sau (qua `/booking`), đợi qua giờ đó mà không xác nhận, tối đa 5 phút sau sẽ thấy nó tự chuyển `Cancelled` (kèm cập nhật realtime nếu đang mở `/my-bookings`).

> **Lưu ý bảo mật**: Hangfire Dashboard hiện **chỉ bật ở môi trường Development** và chưa gắn `Authorization filter` theo đúng quyền Admin (JWT Bearer không tương thích thẳng với cơ chế auth cookie/session riêng của Hangfire Dashboard — cần thêm hạ tầng auth riêng để làm đúng, ngoài phạm vi bài demo). **Không bật dòng `app.UseHangfireDashboard(...)` này ở production** nếu chưa cấu hình filter.

---

## Danh sách chức năng đã hoàn thành

### Bắt buộc

- [x] Đăng nhập JWT, phân quyền Customer/Admin
- [x] CRUD dịch vụ (validate Name/Duration/Price, search, phân trang, khóa/mở khóa)
- [x] Quản lý nhân viên + lịch làm việc (validate `StartTime < EndTime`, chống trùng ca)
- [x] Đặt lịch: tự tính `EndTime`, validate không đặt quá khứ, phải trong giờ làm việc
- [x] Chống trùng lịch đúng công thức bắt buộc, trả `409 Conflict`
- [x] Customer xem/hủy booking của chính mình; Admin xem/xác nhận/hoàn thành/hủy toàn bộ
- [x] Phân quyền kiểm tra độc lập ở backend (không dựa vào frontend)
- [x] 7 màn hình bắt buộc: `/login`, `/services`, `/booking`, `/my-bookings`, `/admin/services`, `/admin/schedules`, `/admin/bookings`
- [x] Loading / Error / Empty state ở mọi màn hình danh sách
- [x] Exception xử lý tập trung, DTO tách biệt Entity, async xuyên suốt, phân trang tại database
- [x] Migration/SQL script, seed data đầy đủ, README

### Phần cộng điểm đã làm

- [x] **Unit test** (`backend/ServiceBooking.Tests`) — 24 test case, tập trung vào chống trùng lịch (Booking + WorkSchedule) và validation, dùng SQLite in-memory để test được cả transaction thật
- [x] **SignalR** — cập nhật booking real-time cho cả `/my-bookings` (Customer) và `/admin/bookings` (Admin), không cần F5
- [x] **Hangfire** — job chạy mỗi 5 phút tự động hủy booking `Pending` quá hạn và tự động hoàn thành booking `Confirmed` đã qua giờ hẹn

### Chưa làm

- [ ] Docker Compose — đã cân nhắc nhưng bỏ qua theo quyết định thực tế (máy phát triển không đủ cấu hình chạy Docker mượt); chạy thủ công theo hướng dẫn ở trên vẫn đầy đủ chức năng
- [ ] Refresh token (hiện chỉ có access token, hết hạn thì yêu cầu đăng nhập lại)
- [ ] Authorization filter riêng cho Hangfire Dashboard theo đúng quyền Admin (hiện chỉ giới hạn bằng cách chỉ bật ở môi trường Development)

---

## Cấu trúc thư mục

```
ServiceBookingSystem/
├── backend/
│   ├── ServiceBooking.API/
│   │   ├── Hubs/BookingHub.cs           # SignalR Hub
│   │   └── Services/
│   │       ├── BookingNotifier.cs        # Bắn sự kiện SignalR sau khi booking thay đổi
│   │       └── OverdueBookingProcessor.cs # Job Hangfire xử lý booking quá hạn
│   └── ServiceBooking.Tests/             # Unit test (xUnit + SQLite in-memory)
├── frontend/
│   └── src/lib/signalr.ts                # Kết nối SignalR dùng chung cho toàn app
├── database/
│   └── schema_and_seed.sql               # SQL script thay thế migration nếu cần
├── postman/
│   └── ServiceBookingSystem.postman_collection.json
└── README.md
```

Tài liệu chi tiết hơn (giải thích từng file, hướng dẫn kiểm thử) nằm ở các file đi kèm ngoài repo này: `01_Tai_Lieu_Cau_Truc_Du_An.md`, `02_Huong_Dan_Kiem_Thu_Swagger.md`, `03_Kiem_Thu_UI_Ngay3_Ngay4.md`, `04_Huong_Dan_Ban_Giao.md`.

---

## Push code lên GitHub

```bash
cd ServiceBookingSystem
git init
git add .
git commit -m "Service Booking Management System - full stack intern demo"
git branch -M main
git remote add origin https://github.com/<username>/<repo-name>.git
git push -u origin main
```

Sau khi push, vào Settings của repo trên GitHub → đổi **Visibility thành Public** nếu repo đang ở chế độ Private (Settings → General → cuộn xuống "Danger Zone" → "Change visibility").

**Trước khi push, kiểm tra lại:**
- `backend/ServiceBooking.API/appsettings.json` không chứa secret thật (giá trị hiện tại chỉ là key demo cho môi trường dev, không phải secret production)
- `frontend/.env.local` **không** được commit (đã có trong `.gitignore`) — chỉ commit `.env.local.example`
- Thư mục `backend/ServiceBooking.API/Migrations` **phải** được commit (không nằm trong `.gitignore`) — nếu không, người khác clone về sẽ không tạo được database
