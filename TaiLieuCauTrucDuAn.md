# Tài liệu cấu trúc dự án — Service Booking Management System

## Kiến trúc tổng quan

Backend theo mô hình **Layered Architecture** 3 tầng:

```
Controller (nhận request, trả response)
      ↓
Service (business logic, validation nghiệp vụ)
      ↓
DbContext / Model (truy cập database)
```

Không đặt logic nghiệp vụ trong Controller — Controller chỉ gọi Service và định dạng response.

---

## Database, cấu trúc Backend, Auth API

### Thư mục `backend/ServiceBooking.API/Models/` — Entity (bảng database)

| File | Ý nghĩa |
|---|---|
| `Enums.cs` | Khai báo 2 enum dùng xuyên suốt hệ thống: `UserRole` (Customer/Admin) và `BookingStatus` (Pending/Confirmed/Completed/Cancelled). Lưu dưới dạng chuỗi trong DB (không phải số) để dễ đọc khi query trực tiếp. |
| `User.cs` | Entity người dùng (cả Customer lẫn Admin dùng chung bảng `Users`, phân biệt bằng `Role`). Có `PasswordHash` (hash BCrypt, không bao giờ trả ra API), `Email` (unique), `IsActive` (khóa tài khoản). |
| `Service.cs` | Entity dịch vụ: `Name`, `Description`, `DurationMinutes` (bắt buộc > 0), `Price` (decimal 18,2, bắt buộc >= 0), `IsActive` (dùng để khóa/mở khóa dịch vụ thay vì xóa cứng). |
| `Staff.cs` | Entity nhân viên: `FullName`, `Email` (unique), `IsActive`. Theo phạm vi đề bài, 1 nhân viên có thể thực hiện mọi dịch vụ (không có bảng ánh xạ Staff-Service). |
| `WorkSchedule.cs` | Entity ca làm việc của nhân viên: `StaffId` (FK), `WorkDate` (kiểu `DateOnly`), `StartTime`/`EndTime` (kiểu `TimeOnly`). Dùng `DateOnly`/`TimeOnly` thay vì `DateTime` để tránh nhầm lẫn timezone khi chỉ cần lưu ngày/giờ thuần túy. |
| `Booking.cs` | Entity trung tâm của hệ thống: `BookingCode` (unique, dạng `BK000001`), `CustomerId`/`ServiceId`/`StaffId` (FK), `StartTime`/`EndTime` (DateTime), `Status`, `CustomerNote`, `CancellationReason`, `CreatedAt`. |

### Thư mục `DTOs/Auth/` — Data Transfer Object cho Authentication

| File | Ý nghĩa |
|---|---|
| `LoginRequestDto.cs` | Dữ liệu request khi đăng nhập: `Email`, `Password`. Có `DataAnnotations` validate email hợp lệ, mật khẩu tối thiểu 6 ký tự. |
| `UserDto.cs` | Dữ liệu user trả về client — **KHÔNG chứa `PasswordHash`**, đây là lý do tách DTO riêng khỏi Entity. |
| `LoginResponseDto.cs` | Response sau khi đăng nhập thành công: `AccessToken` (JWT), `ExpiresAt`, `User` (UserDto). |

### Thư mục `Data/` — Truy cập database

| File | Ý nghĩa |
|---|---|
| `ApplicationDbContext.cs` | DbContext chính của EF Core. Khai báo `DbSet` cho 5 bảng, cấu hình Fluent API: unique index (`Email`, `BookingCode`), `DeleteBehavior.Restrict` cho các FK của Booking (tránh lỗi "multiple cascade paths" của SQL Server), convert enum sang string khi lưu DB, index hỗ trợ truy vấn nhanh (`StaffId+StartTime+EndTime` để check trùng lịch, `Status` để lọc). |
| `SeedData.cs` | Khai báo dữ liệu mẫu tĩnh (`HasData`) được EF Core chèn sẵn khi chạy migration: 1 Admin, 2 Customer, 2 Staff, 5 Services, 14 WorkSchedule (7 ngày × 2 nhân viên), 10 Booking đủ 4 trạng thái. Mật khẩu là hash BCrypt tính sẵn (vì `HasData` yêu cầu giá trị cố định tại thời điểm tạo migration). |

### Thư mục `Services/` (phần Auth)

| File | Ý nghĩa |
|---|---|
| `ITokenService.cs` / `TokenService.cs` | Sinh JWT token từ thông tin `User`: claim `sub` (userId), `email`, `role`, `jti`; ký bằng HMAC-SHA256 với key trong `appsettings.json`. |
| `IAuthService.cs` / `AuthService.cs` | Business logic đăng nhập: tìm user theo email, verify password bằng `BCrypt.Verify`, kiểm tra `IsActive`, gọi `TokenService` sinh token. Cũng có `GetCurrentUserAsync` phục vụ `/api/auth/me`. |

### Thư mục `Controllers/` (phần Auth)

| File | Ý nghĩa |
|---|---|
| `AuthController.cs` | 2 endpoint: `POST /api/auth/login` (public, trả JWT), `GET /api/auth/me` (yêu cầu đăng nhập, đọc claim `sub` từ token để trả thông tin user hiện tại). |

### Thư mục `Middleware/`

| File | Ý nghĩa |
|---|---|
| `ExceptionHandlingMiddleware.cs` | Middleware xử lý exception **tập trung** — bắt toàn bộ exception chưa xử lý trong pipeline, map `ApiException` sang đúng HTTP status code (400/401/403/404/409), map exception lạ khác sang 500 (ẩn chi tiết lỗi thật, chỉ log lại). Trả về response dạng `ApiResponse<T>` thống nhất cho toàn bộ API. |

### Thư mục `Common/`

| File | Ý nghĩa |
|---|---|
| `ApiResponse.cs` | 2 class dùng chung toàn hệ thống: `ApiResponse<T>` (bọc mọi response API: `success`, `message`, `data`, `errors`) và `PagedResult<T>` (bọc danh sách có phân trang: `items`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`). |
| `ApiException.cs` | Exception nghiệp vụ tùy chỉnh, mang theo `HttpStatusCode` mong muốn. Có các static factory tiện dụng: `NotFound()`, `Unauthorized()`, `Forbidden()`, `Conflict()`, `BadRequest()` — dùng trong Service để "ném lỗi" một cách rõ ràng, không cần try/catch thủ công ở Controller. |

### File gốc

| File | Ý nghĩa |
|---|---|
| `Program.cs` | Entry point — cấu hình toàn bộ pipeline: đăng ký DbContext (SQL Server), đăng ký DI cho các Service, cấu hình JWT Authentication + policy phân quyền (`AdminOnly`, `CustomerOnly`), cấu hình Swagger (kèm nút "Authorize" nhập Bearer token), cấu hình CORS cho phép frontend Next.js gọi API, đăng ký middleware xử lý lỗi. |
| `appsettings.json` | Connection string SQL Server, cấu hình JWT (`Key`, `Issuer`, `Audience`, thời hạn token), danh sách origin CORS được phép. |
| `ServiceBooking.API.csproj` | Khai báo target framework .NET 8 và các NuGet package: EF Core + SQL Server provider, JWT Bearer, Swashbuckle (Swagger), BCrypt.Net-Next (hash password). |
| `.gitignore` | Loại trừ `bin/`, `obj/`, file cấu hình nhạy cảm khi đẩy lên Git. |

### Thư mục `database/`

| File | Ý nghĩa |
|---|---|
| `schema_and_seed.sql` | Script SQL thuần tạo schema (5 bảng + PK/FK/unique/check constraint) và chèn sẵn seed data — dùng thay thế cho migration EF Core nếu máy không cài `dotnet-ef`, tương đương 100% với những gì `SeedData.cs` tạo ra. |

---

## Business Logic: Services, Schedules & Bookings

### Thư mục `DTOs/Common/`

| File | Ý nghĩa |
|---|---|
| `PaginationQuery.cs` | Class cha dùng chung cho mọi query có phân trang (`PageNumber`, `PageSize`). Tự "kẹp" `PageSize` trong khoảng hợp lý (mặc định 10, tối đa 100) để tránh client truyền `pageSize=999999` gây tải toàn bộ bảng. |

### Thư mục `DTOs/Services/`

| File | Ý nghĩa |
|---|---|
| `ServiceDto.cs` | Dữ liệu dịch vụ trả về client. |
| `CreateServiceDto.cs` | Dữ liệu tạo dịch vụ mới. Validate: `Name` bắt buộc, `DurationMinutes` > 0 (`[Range(1, int.MaxValue)]`), `Price` >= 0 (`[Range(0, double.MaxValue)]`). |
| `UpdateServiceDto.cs` | Giống `CreateServiceDto` nhưng có thêm `IsActive` — dùng chính field này để khóa/mở khóa dịch vụ (PUT, không có endpoint xóa riêng). |
| `ServiceQueryDto.cs` | Kế thừa `PaginationQuery`, thêm `Search` (tìm theo tên) và `IsActive` (lọc dịch vụ đang mở/khóa) cho `GET /api/services`. |

### Thư mục `DTOs/Staffs/`

| File | Ý nghĩa |
|---|---|
| `StaffDto.cs` | Dữ liệu nhân viên trả về client. |
| `CreateStaffDto.cs` | Dữ liệu tạo nhân viên: `FullName`, `Email` (validate định dạng email). |
| `UpdateStaffDto.cs` | Giống trên, thêm `IsActive` để khóa/mở khóa nhân viên. |
| `WorkScheduleDto.cs` | Dữ liệu ca làm việc trả về client. |
| `CreateWorkScheduleDto.cs` | Dữ liệu tạo ca làm việc mới: `WorkDate`, `StartTime`, `EndTime`. Implement `IValidatableObject` để tự validate `StartTime < EndTime` ngay ở tầng DTO (validate 2 lần: model binding + trong Service, phòng trường hợp Service được gọi trực tiếp). |

### Thư mục `DTOs/Bookings/` (nhóm DTO quan trọng nhất)

| File | Ý nghĩa |
|---|---|
| `BookingDto.cs` | Dữ liệu booking đầy đủ trả về client: bao gồm cả tên khách hàng/dịch vụ/nhân viên (join sẵn), không chỉ trả ID thô. |
| `CreateBookingDto.cs` | Dữ liệu tạo booking: `ServiceId`, `StaffId`, `StartTime`, `CustomerNote`. **Cố tình không có `EndTime`** — đúng yêu cầu "Customer chỉ chọn thời gian bắt đầu". |
| `AvailableSlotDto.cs` | 1 khung giờ trống: `StaffId`, `StaffName`, `StartTime`, `EndTime`. |
| `AvailableSlotsQueryDto.cs` | Query cho `GET /available-slots`: `ServiceId` (bắt buộc), `Date` (bắt buộc), `StaffId` (tùy chọn — không truyền thì trả khung giờ trống của tất cả nhân viên). |
| `BookingFilterDto.cs` | Kế thừa `PaginationQuery`, thêm `Status`, `FromDate`, `ToDate` — dùng chung cho cả `GET /my-bookings` và `GET /api/bookings` (Admin). |
| `UpdateBookingStatusDto.cs` | Dữ liệu cho Admin đổi trạng thái: `Status` (enum), `CancellationReason` (bắt buộc nếu `Status = Cancelled`). |
| `CancelBookingDto.cs` | Dữ liệu hủy booking: chỉ 1 field `Reason`, bắt buộc nhập. |

### Thư mục `Services/`

| File | Ý nghĩa |
|---|---|
| `IServiceManagementService.cs` / `ServiceManagementService.cs` | CRUD dịch vụ. `GetListAsync` thực hiện tìm kiếm (`EF.Functions.Like`) + phân trang **tại database** (`Skip/Take` dịch thành `OFFSET/FETCH`), không tải hết bảng về rồi mới cắt trang. `CreateAsync`/`UpdateAsync` validate lại `DurationMinutes > 0` và `Price >= 0` ở tầng Service (không chỉ dựa vào DataAnnotations). |
| `IStaffManagementService.cs` / `StaffManagementService.cs` | CRUD nhân viên + quản lý ca làm việc. `CreateScheduleAsync` là phần quan trọng: kiểm tra `StartTime < EndTime`, và **chống trùng ca** bằng đúng công thức so sánh khoảng thời gian (`dto.StartTime < ws.EndTime && dto.EndTime > ws.StartTime`) với các ca đã có của cùng nhân viên trong cùng ngày — trùng thì trả `409 Conflict`. |
| `IBookingService.cs` / `BookingService.cs` | **File quan trọng nhất dự án** — chứa toàn bộ business logic Booking: <br>• `GetAvailableSlotsAsync`: sinh danh sách khung giờ ứng viên theo bước nhảy 30 phút trong ca làm việc, loại khung giờ quá khứ và khung giờ đã bị đặt (chưa hủy). <br>• `CreateAsync`: tính `EndTime` tự động, validate không đặt quá khứ, validate nằm trong ca làm việc, **chống trùng lịch đúng công thức bắt buộc của đề bài**, bọc trong transaction `IsolationLevel.Serializable` để giảm race condition khi 2 request đặt cùng lúc, sinh `BookingCode` sau khi có `Id`. <br>• `GetMyBookingsAsync` / `GetAllAsync`: lọc + phân trang tại database. <br>• `UpdateStatusAsync`: chỉ cho Admin chuyển đúng luồng trạng thái hợp lệ (`Pending→Confirmed→Completed`, hoặc `→Cancelled`). <br>• `CancelAsync`: dùng chung hàm `ValidateCancellable` cho cả Customer và Admin — chặn hủy booking đã `Completed` hoặc đã bắt đầu (`StartTime <= now`), bắt buộc có lý do. |

### Thư mục `Controllers/`

| File | Ý nghĩa |
|---|---|
| `ServicesController.cs` | `GET /api/services` (list + search + phân trang, cần đăng nhập), `GET /{id}`, `POST`/`PUT` chỉ Admin (`[Authorize(Roles = "Admin")]`). |
| `StaffsController.cs` | `GET /api/staffs`, `GET /{id}`, `POST`/`PUT` (Admin), `GET /{id}/schedules`, `POST /{id}/schedules` (Admin). |
| `BookingsController.cs` | 6 endpoint: `GET available-slots`, `GET my-bookings` (mọi user, tự lấy `userId` từ token — không nhận `customerId` từ client để tránh xem booking người khác), `GET` toàn bộ (Admin), `POST` tạo booking (chỉ role Customer), `PATCH {id}/status` (chỉ Admin), `POST {id}/cancel` (mọi user đăng nhập, nhưng Service kiểm tra lại quyền sở hữu nếu không phải Admin). |

### Thay đổi trong `Program.cs`

- Đăng ký thêm 3 service vào DI: `IServiceManagementService`, `IStaffManagementService`, `IBookingService`.
- Thêm `JsonStringEnumConverter` vào `AddJsonOptions` — để `Status` trong JSON body được gửi/nhận dạng chuỗi (`"Confirmed"`) thay vì số (`1`), dễ test qua Swagger và dễ tích hợp frontend hơn.

---

## Setup Frontend Next.js & Giao diện Customer

Toàn bộ nằm trong thư mục `frontend/`, dùng **Next.js App Router + TypeScript + TailwindCSS**.

### File cấu hình gốc

| File | Ý nghĩa |
|---|---|
| `package.json` | Khai báo dependency: `next`, `react`, `axios` (gọi API), `js-cookie` (đọc/ghi cookie JWT), `jwt-decode` (decode JWT phía client), `clsx` (nối class có điều kiện); devDependencies gồm `typescript`, `tailwindcss`, `eslint-config-next`. |
| `tsconfig.json` | Cấu hình TypeScript chuẩn Next.js App Router, khai báo alias `@/*` → `src/*` để import gọn (`@/lib/api` thay vì `../../lib/api`). |
| `next.config.js` | Bật `reactStrictMode`. Không cần cấu hình đặc biệt khác ở giai đoạn này. |
| `tailwind.config.ts` | Khai báo **design token riêng** của dự án: bảng màu `stone` (nền), `ink` (chữ), `sage` (accent chính — xanh thư giãn, hợp chủ đề spa/salon), `amber` (nhấn phụ), `rose` (lỗi/hủy); khai báo font family `display` (Fraunces — heading) và `sans` (Inter — UI/body). Mục đích: tránh giao diện rập khuôn "kem-cam" mặc định của AI-generated UI. |
| `postcss.config.js` | Cấu hình plugin `tailwindcss` + `autoprefixer` cho PostCSS. |
| `.env.local.example` | Mẫu biến môi trường `NEXT_PUBLIC_API_URL` — copy thành `.env.local` rồi chỉnh đúng port backend. |
| `.gitignore` | Loại trừ `node_modules/`, `.next/`, `.env.local` khi đẩy Git. |
| `README.md` | Hướng dẫn cài đặt, chạy, tài khoản demo, và giải thích các quyết định kỹ thuật riêng của frontend (chi tiết hơn bảng bên dưới). |

### Thư mục `src/lib/` — Lớp giao tiếp API & tiện ích dùng chung

| File | Ý nghĩa |
|---|---|
| `types.ts` | Toàn bộ TypeScript interface khớp **1-1** với DTO backend (`User`, `Service`, `Staff`, `Booking`, `AvailableSlot`, `ApiResponse<T>`, `PagedResult<T>`, `CreateBookingPayload`...) — mục đích: nếu backend đổi field mà quên đổi FE, TypeScript sẽ báo lỗi biên dịch ngay thay vì lỗi runtime âm thầm. |
| `constants.ts` | Chỉ chứa 1 hằng số `TOKEN_COOKIE_NAME`. Tách riêng khỏi `auth-cookies.ts` để `middleware.ts` (chạy ở **Edge runtime**) không phải import gián tiếp package `js-cookie` (vốn dùng API trình duyệt, không cần thiết và có thể rủi ro tương thích trong Edge runtime). |
| `auth-cookies.ts` | 3 hàm `saveToken` / `getToken` / `clearToken` thao tác với cookie JWT (tên `access_token`), dùng thư viện `js-cookie`. Cookie **không** đặt `httpOnly` — lý do và đánh đổi được ghi chú ngay trong file. |
| `jwt.ts` | `decodeToken()` và `isTokenExpired()` — decode JWT phía client (**không verify chữ ký**, chỉ dùng để đọc claim cho UI như hiển thị tên, kiểm tra hết hạn để chủ động logout sớm). Verify thật sự luôn nằm ở backend. |
| `api-client.ts` | Trái tim của tầng gọi API: tạo `axios` instance với `baseURL` từ biến môi trường; **request interceptor** tự đọc token từ cookie và gắn `Authorization: Bearer …` vào mọi request (tự xóa cookie nếu phát hiện token đã hết hạn trước khi gửi); **response interceptor** bắt lỗi `401` → xóa cookie + điều hướng cứng về `/login`. Có thêm hàm `getErrorMessage()` dùng chung để lấy đúng message tiếng Việt từ `ApiResponse.message` của backend thay vì message mặc định (khó hiểu) của Axios. |
| `api.ts` | Gom toàn bộ lời gọi API theo module: `authApi` (login, getMe), `servicesApi` (getList), `staffsApi` (getList), `bookingsApi` (getAvailableSlots, getMyBookings, create, cancel) — các Component chỉ gọi các hàm này, không tự viết `axios.get(...)` rải rác khắp nơi. |

### Thư mục `src/context/`

| File | Ý nghĩa |
|---|---|
| `AuthContext.tsx` | React Context quản lý state `user` hiện tại toàn app. Khi app khởi động (F5), nếu còn cookie hợp lệ thì gọi `GET /api/auth/me` để khôi phục đúng thông tin user mới nhất từ server (cố tình **không** suy luận user trực tiếp từ payload JWT, để tránh hiển thị sai nếu tài khoản vừa bị Admin khóa ở tab khác). Cung cấp `login()`, `logout()` cho toàn bộ component con qua hook `useAuth()`. |

### File `src/middleware.ts` — Bảo vệ route

| Ý nghĩa |
|---|
| Chạy ở Edge runtime trước khi bất kỳ page nào render. Đọc cookie `access_token`, decode JWT lấy `role`/`exp` (dùng `jwt-decode`, không verify chữ ký). Logic: đã đăng nhập mà vào `/login` → đẩy sang `/services`; route cần đăng nhập mà không có token hợp lệ → đẩy về `/login` (kèm `?redirectTo=` để quay lại đúng trang sau khi đăng nhập); vào `/admin/*` mà không phải role Admin → chặn (dự phòng cho Ngày 4, hiện chưa có trang Admin nào). `matcher` áp dụng cho mọi route trừ static asset. |

### Thư mục `src/components/` — Component dùng chung

| File | Ý nghĩa |
|---|---|
| `Navbar.tsx` | Thanh điều hướng trên cùng: logo, link `Dịch vụ`/`Lịch của tôi` (tự highlight link đang active), tên user, nút Đăng xuất. Tự ẩn hoàn toàn nếu chưa đăng nhập (`user === null`). |
| `LoadingState.tsx` | Spinner + label, dùng thống nhất cho **mọi** màn hình đang tải dữ liệu. |
| `EmptyState.tsx` | Khối hiển thị khi danh sách trống, có `title`, `description` tùy chọn, và `action` tùy chọn (nút hành động, vd "Thử lại"). |
| `ErrorState.tsx` | Khối hiển thị khi gọi API lỗi, có nút "Thử lại" gọi lại hàm fetch tương ứng. |
| `Pagination.tsx` | Nút Trước/Sau + hiển thị "Trang X/Y", tự ẩn nếu chỉ có 1 trang. |
| `StatusBadge.tsx` | Badge màu cho từng trạng thái booking (Pending=amber, Confirmed=sage, Completed=xám trung tính, Cancelled=đỏ), có label tiếng Việt. |
| `ServiceCard.tsx` | 1 dòng hiển thị dịch vụ trong danh sách (tên, mô tả, thời lượng, giá đã format VNĐ) + nút "Đặt lịch" trỏ sang `/booking?serviceId=…`. |
| `CancelBookingModal.tsx` | Modal nhập lý do hủy booking, validate bắt buộc nhập lý do trước khi cho xác nhận, tự disable nút trong lúc đang gửi request. |

### Thư mục `src/hooks/`

| File | Ý nghĩa |
|---|---|
| `useDebounce.ts` | Hook debounce generic, dùng cho ô tìm kiếm dịch vụ ở `/services` (chờ 400ms sau khi người dùng ngừng gõ mới gọi API, tránh gọi API liên tục theo từng phím bấm). |

### Thư mục `src/app/` — Các trang (route)

| File | Route | Ý nghĩa |
|---|---|---|
| `layout.tsx` | *(layout gốc)* | Khai báo font (`Fraunces`, `Inter` qua `next/font/google`), bọc toàn app trong `<AuthProvider>` và render `<Navbar>` cố định trên mọi trang. |
| `globals.css` | — | Tailwind directives + các class dùng chung định nghĩa sẵn (`.btn-primary`, `.btn-secondary`, `.btn-danger`, `.input-field`, `.card`) để mọi form/button trong app đồng nhất style mà không lặp lại class Tailwind dài dòng. |
| `page.tsx` | `/` | Trang fallback tối giản — thực tế `middleware.ts` luôn điều hướng "/" sang `/services` hoặc `/login` trước khi trang này kịp render. |
| `login/page.tsx` + `login/LoginForm.tsx` | `/login` | `page.tsx` chỉ bọc `<Suspense>` (bắt buộc vì `LoginForm` dùng `useSearchParams`). `LoginForm.tsx` là form thật: nhập email/password, gọi `login()` từ `AuthContext`, hiển thị lỗi tiếng Việt, sau khi đăng nhập thành công điều hướng theo `?redirectTo=` (do middleware gắn vào) hoặc mặc định `/services`. Có sẵn khối gợi ý tài khoản demo ngay trên form. |
| `services/page.tsx` | `/services` | Danh sách dịch vụ: ô tìm kiếm (debounce), gọi `servicesApi.getList({ isActive: true, search, pageNumber, pageSize })`, xử lý đủ 3 trạng thái Loading/Empty/Error, phân trang. |
| `booking/page.tsx` + `booking/BookingForm.tsx` | `/booking` | Trang đặt lịch — **phức tạp nhất**: tải sẵn danh sách dịch vụ + nhân viên; nếu vào từ nút "Đặt lịch" ở `/services` (`?serviceId=…`) thì tự chọn sẵn đúng dịch vụ đó; chọn ngày (giới hạn `min` = hôm nay); nhân viên là **bộ lọc tùy chọn** (không bắt buộc chọn trước); gọi `bookingsApi.getAvailableSlots()` mỗi khi dịch vụ/ngày/nhân viên thay đổi; người dùng **click trực tiếp vào 1 khung giờ** trong lưới kết quả — hành động này xác định luôn cả `staffId` lẫn `startTime` chính xác (tránh trường hợp chọn nhân viên A nhưng lại bấm giờ trống của nhân viên B); có ô ghi chú; nút "Xác nhận đặt lịch" gọi `bookingsApi.create()`, nếu backend trả `409` (trùng lịch do người khác đặt trước) thì hiển thị lỗi và tự gọi lại `available-slots`. |
| `my-bookings/page.tsx` + `my-bookings/BookingsList.tsx` | `/my-bookings` | Danh sách booking của khách hàng đang đăng nhập: filter theo trạng thái (dạng pill button), phân trang, mỗi booking hiển thị đủ thông tin (mã, dịch vụ, nhân viên, thời gian, ghi chú, lý do hủy nếu có); nút "Hủy lịch" chỉ hiện với booking `Pending`/`Confirmed`, mở `CancelBookingModal`; sau khi tạo booking thành công từ `/booking`, trang này hiển thị banner thông báo (đọc từ `?created=1`). |

### Bảng tổng hợp: file nào giải quyết yêu cầu nào (mục 6 và 8.2 đề bài)

| Yêu cầu trong đề bài | File triển khai |
|---|---|
| `/login`: form đăng nhập, lưu token, hiển thị lỗi | `login/LoginForm.tsx`, `lib/auth-cookies.ts`, `context/AuthContext.tsx` |
| `/services`: danh sách, phân trang, search, nút đặt lịch | `services/page.tsx`, `components/ServiceCard.tsx`, `components/Pagination.tsx` |
| `/booking`: chọn dịch vụ/nhân viên/ngày/khung giờ/ghi chú | `booking/BookingForm.tsx` |
| `/my-bookings`: danh sách, lọc trạng thái, modal hủy | `my-bookings/BookingsList.tsx`, `components/CancelBookingModal.tsx` |
| HTTP Client tự đính JWT + xử lý token hết hạn | `lib/api-client.ts` |
| Auth Context / Middleware bảo vệ route | `context/AuthContext.tsx`, `src/middleware.ts` |
| Loading / Error / Empty state | `components/LoadingState.tsx`, `ErrorState.tsx`, `EmptyState.tsx` — áp dụng ở cả 3 trang chính |
| Không đưa secret/connection string xuống client | Chỉ `NEXT_PUBLIC_API_URL` (không nhạy cảm) nằm trong biến môi trường `NEXT_PUBLIC_*`; mọi secret (JWT key, connection string) chỉ tồn tại ở backend |
| Responsive cơ bản | Tailwind responsive utility (`md:grid-cols-2` ở `BookingForm.tsx`, layout max-width ở `layout.tsx`) |

---

## Trang quản trị Admin & Rà soát kiểm thử

Toàn bộ nằm trong `frontend/src/app/admin/`

### File mới: `src/components/Modal.tsx`

| Ý nghĩa |
|---|
| Component overlay + khung modal dùng chung cho mọi form Admin (nền mờ phủ toàn màn hình, khung trắng bo góc, nút ✕ đóng ở góc, tự cuộn nếu nội dung dài). Tách riêng khỏi `CancelBookingModal.tsx` (vốn tự vẽ overlay riêng từ Ngày 3) để `ServiceFormModal` và `StaffFormModal` không phải lặp lại cùng đoạn CSS overlay. |

### Thư mục `src/app/admin/services/`

| File | Ý nghĩa |
|---|---|
| `page.tsx` | Trang `/admin/services`: bảng (table) toàn bộ dịch vụ (khác `/services` của Customer — Admin thấy **cả dịch vụ đã khóa**), có ô tìm kiếm debounce, 3 pill filter Tất cả/Đang mở/Đã khóa, phân trang. Nút "Khóa"/"Mở khóa" ngay trên mỗi dòng gọi thẳng `servicesApi.update()` với `isActive` đảo ngược (dùng lại dữ liệu hiện có trong state, **không** cần mở form fetch lại) — thao tác nhanh 1 click, có loading cục bộ theo từng dòng (`togglingId`) để tránh disable nhầm toàn bộ bảng. Nút "Sửa" mở `ServiceFormModal` ở chế độ edit. |
| `ServiceFormModal.tsx` | Form modal dùng chung cho cả **Tạo mới** và **Sửa** (phân biệt qua prop `service: Service \| null`). Validate lại phía client đúng 3 quy tắc backend (tên bắt buộc, thời lượng > 0, giá ≥ 0) để báo lỗi tức thì, không cần chờ round-trip API — nhưng backend vẫn là nơi validate quyết định cuối cùng. Khi edit mới hiện checkbox "Đang mở cho đặt lịch" (vì `POST /services` không nhận field `isActive` — backend luôn tạo dịch vụ mới ở trạng thái mở). |

### Thư mục `src/app/admin/schedules/`

| File | Ý nghĩa |
|---|---|
| `page.tsx` | Trang `/admin/schedules`: layout 2 cột. Cột trái là danh sách nhân viên (bấm để chọn, có nút "Sửa" nhỏ mở `StaffFormModal`); tự động chọn nhân viên đầu tiên khi tải xong, và tự chuyển sang nhân viên khác nếu nhân viên đang chọn không còn trong danh sách (vd sau khi lọc). Cột phải render `StaffSchedulePanel` cho nhân viên đang chọn. Nút "+ Thêm nhân viên" ở góc trên mở `StaffFormModal` ở chế độ tạo mới. |
| `StaffFormModal.tsx` | Form modal tạo/sửa nhân viên (Họ tên, Email), tương tự `ServiceFormModal` — khi edit mới hiện checkbox khóa/mở khóa. |
| `StaffSchedulePanel.tsx` | Phần quan trọng nhất của trang này: hiển thị thông tin + trạng thái của 1 nhân viên, kèm **form thêm ca làm việc** (chọn ngày/giờ bắt đầu/giờ kết thúc) và **danh sách ca làm việc sắp tới** (chỉ lấy từ hôm nay trở đi qua `staffsApi.getSchedules(staffId, todayIso())` để danh sách gọn, không hiển thị ca đã qua). Validate `StartTime < EndTime` ngay ở client trước khi gọi API; nếu backend trả `409` (trùng ca — mục 3 đề bài) thì hiển thị đúng message gốc từ backend ngay dưới form, không tự diễn giải lại. |

### Thư mục `src/app/admin/bookings/`

| File | Ý nghĩa |
|---|---|
| `page.tsx` | Trang `/admin/bookings`: khác biệt cốt lõi so với `/my-bookings` của Customer là gọi `bookingsApi.getAll()` (endpoint `GET /api/bookings`, chỉ Admin gọi được) thay vì `getMyBookings()` — nên thấy **booking của mọi khách hàng**, mỗi dòng hiển thị thêm tên khách hàng. Có thêm bộ lọc khoảng ngày (Từ ngày/Đến ngày) bên cạnh filter trạng thái. Nút hành động hiện **có điều kiện theo đúng trạng thái hiện tại** của từng booking: `Pending` → nút "Xác nhận" (gọi `bookingsApi.updateStatus(id, {status: 'Confirmed'})`) + "Hủy"; `Confirmed` → "Hoàn thành" (`status: 'Completed'`) + "Hủy"; `Completed`/`Cancelled` → **không còn nút hành động nào** — chính cách ẩn nút này là lớp bảo vệ đầu tiên cho TC5/TC6 (Customer không có quyền, booking đã chốt không thể sửa), trước cả khi request chạm tới backend. Nút "Hủy" tái sử dụng lại đúng `CancelBookingModal.tsx` đã viết từ Ngày 3 (component này vốn được thiết kế generic, không gắn cứng với Customer). |

### Các file được sửa lại để hỗ trợ Admin

| File | Thay đổi |
|---|---|
| `lib/types.ts` | Thêm `WorkSchedule`, và các payload type cho Admin: `ServicePayload`, `StaffPayload`, `CreateWorkSchedulePayload`, `UpdateBookingStatusPayload`. |
| `lib/api.ts` | Thêm `servicesApi.create/update`, `staffsApi.create/update/getSchedules/createSchedule`, `bookingsApi.getAll` (gọi `GET /api/bookings` — Admin), `bookingsApi.updateStatus` (gọi `PATCH /api/bookings/{id}/status`). |
| `components/Navbar.tsx` | Tách `CUSTOMER_LINKS` và `ADMIN_LINKS`, chọn danh sách hiển thị theo `user.role`; thêm badge "ADMIN" nhỏ cạnh tên khi đăng nhập bằng tài khoản Admin. |
| `context/AuthContext.tsx` | Hàm `login()` giờ **trả về `User`** vừa đăng nhập (trước đây trả `void`) — để `LoginForm` biết ngay role mà điều hướng đúng trang, không cần đợi context re-render rồi mới đọc `user`. |
| `app/login/LoginForm.tsx` | Sau đăng nhập, điều hướng Admin vào `/admin/services`, Customer vào `/services` (trừ khi có `?redirectTo=` từ middleware thì ưu tiên theo đó). |
| `src/middleware.ts` | 2 chỗ redirect "đã đăng nhập mà vào `/login`" và "vào `/` khi đã đăng nhập" đều đổi từ cứng `/services` sang tính theo `payload.role` (Admin → `/admin/services`). Guard `/admin/*` chỉ cho role Admin đã được viết sẵn từ Ngày 3 làm dự phòng — Ngày 4 là lúc nó **thực sự được dùng tới**. |

### Bảng tổng hợp: file nào giải quyết yêu cầu nào

| Yêu cầu trong đề bài | File triển khai |
|---|---|
| `/admin/services`: quản lý, thêm, sửa, khóa/mở khóa dịch vụ | `admin/services/page.tsx`, `admin/services/ServiceFormModal.tsx` |
| `/admin/schedules`: thiết lập lịch làm việc cho từng nhân viên | `admin/schedules/page.tsx`, `StaffFormModal.tsx`, `StaffSchedulePanel.tsx` |
| `/admin/bookings`: xem toàn bộ booking, lọc ngày/trạng thái, xác nhận/hoàn thành/hủy | `admin/bookings/page.tsx` |
| Phân quyền Admin ở tầng route (không chỉ ẩn UI) | `src/middleware.ts` (chặn `/admin/*` với role ≠ Admin) — kết hợp với backend `[Authorize(Roles = "Admin")]` đã có từ Ngày 2, tạo 2 lớp bảo vệ độc lập |
| Test case TC1–TC6 (mục 10 đề bài) — chạy qua UI | Xem tài liệu riêng `03_Kiem_Thu_UI_Ngay3_Ngay4.md` |

---

## Phần cộng điểm & Hoàn thiện README

### Thư mục `backend/ServiceBooking.Tests/` (dự án mới — Unit Test)

| File | Ý nghĩa |
|---|---|
| `ServiceBooking.Tests.csproj` | Project test riêng biệt (không gộp vào `ServiceBooking.API` để tránh lẫn code test vào bản publish thật). Tham chiếu `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.Sqlite`, và `ProjectReference` sang `ServiceBooking.API.csproj` để gọi thẳng các Service thật (không mock). |
| `TestDbContextFactory.cs` | Factory tạo `ApplicationDbContext` cô lập cho từng test, backed bởi **SQLite in-memory** (`Filename=:memory:`, giữ `SqliteConnection` mở suốt vòng đời context). Lý do không dùng `Microsoft.EntityFrameworkCore.InMemory`: provider đó **không hỗ trợ transaction thật** (`Database.BeginTransactionAsync` sẽ ném lỗi), trong khi `BookingService.CreateAsync` bắt buộc dùng transaction `IsolationLevel.Serializable` để chống trùng lịch — đây chính là phần logic quan trọng nhất cần test nên không thể né tránh. Gọi `Database.EnsureCreated()` để dựng schema trực tiếp từ model (bao gồm cả `HasData` seed trong `SeedData.cs`) mà không cần thư mục Migrations — mỗi test tự có sẵn đúng bộ dữ liệu mẫu (2 Customer, 2 Staff, 5 Service...). |
| `BookingServiceTests.cs` | Bộ test quan trọng nhất — **16 test case**, bám sát trực tiếp TC1/TC2/TC3/TC4/TC6 trong đề bài. Điểm đáng chú ý: mọi mốc thời gian dùng trong test đều tính **tương đối** theo `DateTime.Now` (vd: "10 ngày sau hôm nay") thay vì ngày cố định, để bộ test không bao giờ tự hỏng khi chạy vào một ngày khác — khác với `WorkSchedule`/`Booking` tĩnh trong `SeedData.cs` (vốn chỉ để demo UI, có thể "hết hạn" theo thời gian). Có test riêng cho từng biên quan trọng: `NewStart == ExistingEnd` phải **không** bị coi là trùng lịch (đúng công thức bắt buộc của đề bài), booking đã `Cancelled` không tính là chiếm chỗ, không thể chuyển trạng thái bỏ bước (`Pending` → `Completed` thẳng), không hủy được booking đã bắt đầu dù vẫn đang `Pending`. |
| `StaffManagementServiceTests.cs` | 4 test case cho logic chống trùng ca làm việc (mục 3 đề bài): `StartTime < EndTime`, 2 ca chồng lấn cùng nhân viên → `409`, 2 ca liền kề (không chồng lấn) → hợp lệ, cùng khung giờ nhưng khác nhân viên → không bị chặn (xác nhận điều kiện `WHERE StaffId = ...` trong query overlap là đúng, không lỡ tay chặn nhầm toàn hệ thống). |
| `ServiceManagementServiceTests.cs` | 4 test case validate `DurationMinutes > 0`, `Price >= 0` (kể cả biên `Price = 0` phải hợp lệ, chỉ cấm số âm), và xác nhận `UpdateAsync` dùng để khóa/mở khóa dịch vụ qua field `IsActive`. |

> **Lưu ý về TC5**: "Customer không tự xác nhận/hoàn thành được booking" được enforce bằng `[Authorize(Roles = "Admin")]` ở tầng **Controller** (HTTP pipeline), không nằm trong logic bên trong `BookingService` — nên đúng bản chất không thể viết unit test cấp Service cho quy tắc này.

### SignalR — cập nhật booking real-time

| File | Ý nghĩa |
|---|---|
| `Hubs/BookingHub.cs` | Hub SignalR duy nhất của hệ thống (`/hubs/bookings`), chỉ dùng để đẩy sự kiện server → client, không có method nào cho client gọi ngược lên. `OnConnectedAsync` tự xếp connection vào group `Customer-{userId}` (mọi user) và thêm vào group `Admins` nếu role là Admin — nhờ vậy 1 sự kiện có thể nhắm đúng đối tượng cần nhận mà không phải gửi broadcast toàn bộ. |
| `Services/BookingMappings.cs` | Tách `Expression<Func<Booking, BookingDto>>` (trước đây là field private trong `BookingService`) ra file `static class` riêng, để cả `BookingService` lẫn `OverdueBookingProcessor` (job Hangfire) dùng chung đúng 1 logic map Booking → DTO, tránh lặp code và tránh lệch dữ liệu giữa 2 nơi. |
| `Services/BookingNotifier.cs` | `IBookingNotifier`/`BookingNotifier` — bọc `IHubContext<BookingHub>` thành 1 service riêng thay vì inject thẳng vào `BookingService`, giúp tầng nghiệp vụ không phụ thuộc trực tiếp vào SignalR (dễ tắt/thay cơ chế realtime sau này mà không đụng vào logic đặt lịch). `NotifyBookingChangedAsync` gửi sự kiện `"BookingChanged"` tới cả group `Admins` và group `Customer-{customerId}` của đúng khách hàng sở hữu booking đó. |
| `BookingService.cs` (sửa) | Constructor nhận thêm `IBookingNotifier`. Sau khi `CreateAsync`/`UpdateStatusAsync`/`CancelAsync` lưu DB thành công, gọi `_notifier.NotifyBookingChangedAsync(result)` trước khi return — đảm bảo mọi đường thay đổi booking (dù do Customer tạo, Admin xác nhận, hay tự hủy) đều bắn realtime nhất quán, không sót đường nào. |
| `Program.cs` (sửa) | Thêm `AddSignalR().AddJsonProtocol(...)` (cấu hình camelCase + enum-as-string giống hệt REST API, để frontend xử lý payload nhận qua Hub và qua axios theo cùng 1 kiểu dữ liệu). Thêm `MapHub<BookingHub>("/hubs/bookings")`. Quan trọng nhất: thêm `JwtBearerEvents.OnMessageReceived` để đọc JWT từ query string `access_token` — vì WebSocket trình duyệt không cho đính custom header lúc kết nối, đây là cách chính thức Microsoft khuyến nghị cho SignalR + JWT; đoạn code chỉ áp dụng cho path `/hubs/*`, các REST endpoint khác vẫn bắt buộc header `Authorization` như cũ. |
| `frontend/src/lib/signalr.ts` | Tạo **1 kết nối SignalR dùng chung (singleton)** cho toàn app (tránh mở nhiều WebSocket trùng lặp khi chuyển trang), gắn JWT qua `accessTokenFactory` (luôn lấy token mới nhất từ cookie, không hard-code 1 lần). `ensureBookingHubStarted()` an toàn khi gọi nhiều lần từ nhiều component khác nhau nhờ kiểm tra `state` trước khi `start()`. |
| `frontend/.../my-bookings/BookingsList.tsx`, `frontend/.../admin/bookings/page.tsx` (sửa) | Thêm 1 `useEffect` lắng nghe sự kiện `"BookingChanged"` từ Hub, cứ có sự kiện là gọi lại `fetchBookings()` — đơn giản, không cố merge state cục bộ theo từng field, đủ tốt cho phạm vi demo và tránh bug lệch state. |

### Hangfire — tự động xử lý booking quá hạn

| File | Ý nghĩa |
|---|---|
| `Services/IOverdueBookingProcessor.cs` / `OverdueBookingProcessor.cs` | Job chạy định kỳ, xử lý 2 tình huống: (1) booking `Pending` mà `StartTime` đã qua → tự động chuyển `Cancelled` kèm lý do hệ thống; (2) booking `Confirmed` mà `EndTime` đã qua (Admin quên bấm "Hoàn thành") → tự động chuyển `Completed`. Dùng lại `BookingMappings.ProjectToDto` để build DTO cho từng booking vừa bị xử lý, rồi gọi `IBookingNotifier` bắn sự kiện SignalR — thể hiện rõ 2 phần cộng điểm **phối hợp với nhau** chứ không tách rời: job chạy nền vẫn khiến UI đang mở cập nhật tức thời. |
| `Program.cs` (sửa) | `AddHangfire(...)` cấu hình `UseSqlServerStorage` dùng lại đúng `DefaultConnection` (Hangfire tự tạo bảng riêng tiền tố `HangFire.*` trong cùng database, không cần DB riêng cho phạm vi demo). `AddHangfireServer()` để có worker thực sự chạy job (nếu thiếu dòng này, job được đăng ký nhưng không bao giờ chạy). `RecurringJob.AddOrUpdate<IOverdueBookingProcessor>(...)` đăng ký job chạy mỗi 5 phút (cron `*/5 * * * *`). `UseHangfireDashboard("/hangfire")` chỉ bật khi `app.Environment.IsDevelopment()` — lý do: Dashboard dùng cơ chế auth cookie/session riêng của Hangfire, không tái sử dụng thẳng được JWT Bearer của API, nên chưa gắn đúng quyền Admin được trong phạm vi bài demo; bật vô điều kiện ở production sẽ lộ dashboard cho bất kỳ ai truy cập được URL. |

### Postman & Swagger

| File | Ý nghĩa |
|---|---|
| `postman/ServiceBookingSystem.postman_collection.json` | Postman Collection v2.1 đầy đủ toàn bộ API (Auth, Services, Staffs, Bookings), tổ chức theo folder khớp với cấu trúc Controller. 2 request "Login - Admin" và "Login - Customer" có gắn sẵn **Test script** (`pm.collectionVariables.set(...)`) tự động lưu token vào biến `adminToken`/`customerToken` của collection ngay sau khi login thành công — các request còn lại tham chiếu `{{adminToken}}`/`{{customerToken}}` trong header Authorization nên không cần copy-paste token thủ công. Tương tự, request "Create Booking" tự lưu `bookingId` để các request Confirm/Complete/Cancel phía sau dùng lại luôn. |
| Swagger JSON | Không xuất sẵn thành file tĩnh trong repo (vì nó được `Swashbuckle` sinh **runtime** dựa trên code hiện tại của Controller — xuất tĩnh ra dễ bị lệch nếu code đổi mà quên xuất lại). README hướng dẫn lấy trực tiếp tại `/swagger/v1/swagger.json` khi backend đang chạy. |

