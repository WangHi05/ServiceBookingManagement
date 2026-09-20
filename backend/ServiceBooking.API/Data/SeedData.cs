using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Data;

/// <summary>
/// Seed data tĩnh dùng cho HasData (EF Core Migration).
/// Lưu ý: Vì HasData yêu cầu giá trị cố định tại thời điểm tạo migration,
/// các mật khẩu bên dưới là hash BCrypt đã được tính sẵn cho:
///   - Admin:    Admin@123
///   - Customer: Customer@123
/// Ngày làm việc/booking dùng mốc thời gian cố định (2026-09-15 trở đi) để minh họa;
/// Admin có thể tạo thêm lịch làm việc mới qua API bất cứ lúc nào.
/// </summary>
public static class SeedData
{
    private const string AdminPasswordHash = "$2b$11$8eHBHt/uDocDWkhSuExsJeCCqHt0Zwi17kdIAxLV4kJG.Q1gcqgUK"; // Admin@123
    private const string CustomerPasswordHash = "$2b$11$LdAU0Mx2RLKLlBT1ur3oK.du2VYk6.LKhdYV5Om4H64j4bdJncrWW"; // Customer@123

    private static readonly DateTime FixedCreatedAt = new(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder modelBuilder)
    {
        // ---------- Users (1 Admin + 2 Customer) ----------
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FullName = "System Admin",
                Email = "admin@bookingdemo.com",
                PasswordHash = AdminPasswordHash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = FixedCreatedAt
            },
            new User
            {
                Id = 2,
                FullName = "Nguyen Van A",
                Email = "customer1@bookingdemo.com",
                PasswordHash = CustomerPasswordHash,
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = FixedCreatedAt
            },
            new User
            {
                Id = 3,
                FullName = "Tran Thi B",
                Email = "customer2@bookingdemo.com",
                PasswordHash = CustomerPasswordHash,
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = FixedCreatedAt
            }
        );

        // ---------- Staffs (2) ----------
        modelBuilder.Entity<Staff>().HasData(
            new Staff { Id = 1, FullName = "Le Van Staff", Email = "staff1@bookingdemo.com", IsActive = true },
            new Staff { Id = 2, FullName = "Pham Thi Staff", Email = "staff2@bookingdemo.com", IsActive = true }
        );

        // ---------- Services (5) ----------
        modelBuilder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Cắt tóc nam", Description = "Cắt gọn gàng, tạo kiểu cơ bản", DurationMinutes = 30, Price = 100_000, IsActive = true },
            new Service { Id = 2, Name = "Uốn tóc", Description = "Uốn nếp tự nhiên", DurationMinutes = 90, Price = 350_000, IsActive = true },
            new Service { Id = 3, Name = "Nhuộm tóc", Description = "Nhuộm màu theo yêu cầu", DurationMinutes = 60, Price = 300_000, IsActive = true },
            new Service { Id = 4, Name = "Gội đầu dưỡng sinh", Description = "Massage thư giãn da đầu", DurationMinutes = 45, Price = 150_000, IsActive = true },
            new Service { Id = 5, Name = "Spa da mặt", Description = "Chăm sóc và làm sạch da mặt", DurationMinutes = 60, Price = 400_000, IsActive = true }
        );

        // ---------- WorkSchedules (7 ngày x 2 nhân viên, 08:00 - 17:00) ----------
        var workScheduleId = 1;
        var scheduleList = new List<WorkSchedule>();
        var startDate = new DateOnly(2026, 9, 15);

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = startDate.AddDays(dayOffset);

            scheduleList.Add(new WorkSchedule
            {
                Id = workScheduleId++,
                StaffId = 1,
                WorkDate = date,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0)
            });

            scheduleList.Add(new WorkSchedule
            {
                Id = workScheduleId++,
                StaffId = 2,
                WorkDate = date,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0)
            });
        }

        modelBuilder.Entity<WorkSchedule>().HasData(scheduleList);

        // ---------- Bookings (10, nhiều trạng thái, không trùng giờ cùng staff) ----------
        modelBuilder.Entity<Booking>().HasData(
            new Booking
            {
                Id = 1, BookingCode = "BK000001", CustomerId = 2, ServiceId = 1, StaffId = 1,
                StartTime = new DateTime(2026, 9, 15, 9, 0, 0), EndTime = new DateTime(2026, 9, 15, 9, 30, 0),
                Status = BookingStatus.Completed, CustomerNote = "Cắt gọn nhẹ", CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 2, BookingCode = "BK000002", CustomerId = 2, ServiceId = 3, StaffId = 1,
                StartTime = new DateTime(2026, 9, 15, 10, 0, 0), EndTime = new DateTime(2026, 9, 15, 11, 0, 0),
                Status = BookingStatus.Confirmed, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 3, BookingCode = "BK000003", CustomerId = 3, ServiceId = 2, StaffId = 2,
                StartTime = new DateTime(2026, 9, 15, 9, 0, 0), EndTime = new DateTime(2026, 9, 15, 10, 30, 0),
                Status = BookingStatus.Completed, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 4, BookingCode = "BK000004", CustomerId = 3, ServiceId = 4, StaffId = 2,
                StartTime = new DateTime(2026, 9, 16, 8, 0, 0), EndTime = new DateTime(2026, 9, 16, 8, 45, 0),
                Status = BookingStatus.Cancelled, CancellationReason = "Khách bận đột xuất", CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 5, BookingCode = "BK000005", CustomerId = 2, ServiceId = 5, StaffId = 1,
                StartTime = new DateTime(2026, 9, 16, 13, 0, 0), EndTime = new DateTime(2026, 9, 16, 14, 0, 0),
                Status = BookingStatus.Pending, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 6, BookingCode = "BK000006", CustomerId = 3, ServiceId = 1, StaffId = 1,
                StartTime = new DateTime(2026, 9, 17, 9, 0, 0), EndTime = new DateTime(2026, 9, 17, 9, 30, 0),
                Status = BookingStatus.Confirmed, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 7, BookingCode = "BK000007", CustomerId = 2, ServiceId = 3, StaffId = 2,
                StartTime = new DateTime(2026, 9, 17, 14, 0, 0), EndTime = new DateTime(2026, 9, 17, 15, 0, 0),
                Status = BookingStatus.Pending, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 8, BookingCode = "BK000008", CustomerId = 3, ServiceId = 2, StaffId = 1,
                StartTime = new DateTime(2026, 9, 18, 10, 0, 0), EndTime = new DateTime(2026, 9, 18, 11, 30, 0),
                Status = BookingStatus.Cancelled, CancellationReason = "Đổi lịch khác", CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 9, BookingCode = "BK000009", CustomerId = 2, ServiceId = 4, StaffId = 2,
                StartTime = new DateTime(2026, 9, 18, 15, 0, 0), EndTime = new DateTime(2026, 9, 18, 15, 45, 0),
                Status = BookingStatus.Confirmed, CreatedAt = FixedCreatedAt
            },
            new Booking
            {
                Id = 10, BookingCode = "BK000010", CustomerId = 3, ServiceId = 5, StaffId = 1,
                StartTime = new DateTime(2026, 9, 19, 11, 0, 0), EndTime = new DateTime(2026, 9, 19, 12, 0, 0),
                Status = BookingStatus.Pending, CreatedAt = FixedCreatedAt
            }
        );
    }
}
