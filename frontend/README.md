# Frontend — Service Booking Salon (Next.js)
## Yêu cầu môi trường

- Node.js 18.18+ (khuyến nghị 20 LTS)
- Backend API đã chạy (xem README ở thư mục gốc dự án)

## Cài đặt & chạy

```bash
cd frontend
npm install
cp .env.local.example .env.local
```

Mở `.env.local`, chỉnh `NEXT_PUBLIC_API_URL` đúng port backend đang chạy (xem port hiện ở terminal `dotnet run`), ví dụ:

```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

> **Quan trọng**: đảm bảo `Cors:AllowedOrigins` trong `backend/ServiceBooking.API/appsettings.json` có chứa `http://localhost:3000` (giá trị mặc định đã có sẵn).

Chạy dev server:

```bash
npm run dev
```

Mở `http://localhost:3000`.

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Customer | customer1@bookingdemo.com | Customer@123 |
| Customer | customer2@bookingdemo.com | Customer@123 |
| Admin | admin@bookingdemo.com | Admin@123 (chưa có giao diện Admin — sẽ làm ở Ngày 4) |

## Cấu trúc thư mục

```
src/
├── app/
│   ├── layout.tsx          # Root layout: font, AuthProvider, Navbar
│   ├── page.tsx             # "/" - fallback, middleware tự điều hướng
│   ├── globals.css          # Tailwind + design tokens riêng
│   ├── login/               # /login - form đăng nhập
│   ├── services/             # /services - danh sách dịch vụ, search + phân trang
│   ├── booking/               # /booking - form đặt lịch, chọn khung giờ trống
│   └── my-bookings/           # /my-bookings - danh sách + hủy booking
├── components/               # Component dùng chung (Navbar, StatusBadge, EmptyState...)
├── context/AuthContext.tsx    # Auth state phía client (user hiện tại, login/logout)
├── lib/
│   ├── api-client.ts           # Axios instance: tự đính JWT, tự xử lý 401/token hết hạn
│   ├── api.ts                    # Các hàm gọi API theo module (auth/services/staffs/bookings)
│   ├── auth-cookies.ts             # Đọc/ghi/xóa cookie chứa JWT
│   ├── constants.ts                 # Hằng số dùng chung (tên cookie) - tách riêng cho middleware
│   ├── jwt.ts                         # Decode JWT phía client (chỉ phục vụ UI, không dùng để verify)
│   └── types.ts                        # TypeScript type khớp 1-1 với DTO backend
├── hooks/useDebounce.ts        # Debounce ô tìm kiếm
└── middleware.ts               # Bảo vệ route dựa trên cookie JWT
```

## Quyết định kỹ thuật

- **Lưu token**: cookie thường (không `httpOnly`) tên `access_token`. Middleware Next.js (chạy phía server, đọc được mọi cookie bất kể `httpOnly`) dùng cookie này để chặn truy cập route khi chưa đăng nhập; đồng thời client JS cũng đọc được cookie này để tự đính `Authorization: Bearer <token>` vào mọi request qua Axios interceptor. **Đánh đổi đã biết**: cookie không `httpOnly` có thể bị đọc bởi script khác nếu site dính XSS — chấp nhận được cho phạm vi demo, ghi rõ trong code (`lib/auth-cookies.ts`) để dễ nâng cấp sau (chuyển sang BFF proxy + cookie `httpOnly` thật).
- **Token hết hạn**: backend hiện chưa có refresh token. Khi token hết hạn, `api-client.ts` phát hiện qua 2 lớp: (1) tự kiểm tra `exp` trong JWT trước khi gửi request, xóa cookie nếu đã hết hạn; (2) nếu backend vẫn trả `401` (token bị thu hồi, tài khoản bị khóa,...), interceptor tự xóa cookie và điều hướng cứng về `/login`.
- **Middleware bảo vệ route**: đọc cookie, decode JWT (không verify chữ ký — chỉ để lấy `role`/`exp`, verify thật vẫn do backend đảm nhiệm), chặn vào `/login` khi đã đăng nhập, chặn vào các route còn lại khi chưa đăng nhập, và chặn `/admin/*` nếu không phải role Admin (dự phòng cho Ngày 4).
- **available-slots → chọn khung giờ**: thay vì tách rời "chọn nhân viên" và "chọn khung giờ" thành 2 bước độc lập (dễ tạo trạng thái không hợp lệ), UI cho chọn nhân viên như một **bộ lọc tùy chọn**; hành động click vào 1 khung giờ cụ thể mới là thứ xác định chính xác `staffId` + `startTime` gửi lên khi tạo booking — loại bỏ khả năng người dùng chọn nhân viên A nhưng lại chọn giờ trống của nhân viên B.
- **3 trạng thái Loading/Empty/Error**: áp dụng nhất quán ở cả 3 trang `/services`, `/booking`, `/my-bookings` qua 3 component dùng chung `LoadingState`, `EmptyState`, `ErrorState` (đều có nút "Thử lại" ở trạng thái lỗi).
- **Race condition khi đặt lịch**: nếu BE trả `409 Conflict` (khung giờ vừa bị người khác đặt trước), FE hiển thị lỗi rõ ràng và tự động gọi lại `available-slots` để danh sách khung giờ cập nhật ngay, không để người dùng bấm lại vào khung giờ đã mất.


