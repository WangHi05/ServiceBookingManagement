using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        // ---------- Service ----------
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(s => s.Name);
        });

        // ---------- Staff ----------
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(s => s.Email).IsUnique();
        });

        // ---------- WorkSchedule ----------
        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasOne(ws => ws.Staff)
                  .WithMany(s => s.WorkSchedules)
                  .HasForeignKey(ws => ws.StaffId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Không bắt buộc unique index cứng ở DB cho "không trùng ca làm việc"
            // vì việc kiểm tra chồng lấp khung giờ cần logic so sánh StartTime/EndTime,
            // sẽ được enforce ở tầng Service (business logic), không thể biểu diễn bằng
            // 1 unique index đơn giản. Index dưới đây chỉ hỗ trợ truy vấn nhanh theo StaffId + ngày.
            entity.HasIndex(ws => new { ws.StaffId, ws.WorkDate });
        });

        // ---------- Booking ----------
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.BookingCode).IsUnique();
            entity.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(b => b.Customer)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(b => b.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict); // tránh multiple cascade paths

            entity.HasOne(b => b.Service)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(b => b.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Staff)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(b => b.StaffId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Hỗ trợ query lọc theo staff + khoảng thời gian khi check trùng lịch (mục 5.3)
            entity.HasIndex(b => new { b.StaffId, b.StartTime, b.EndTime });
            entity.HasIndex(b => b.Status);
        });

        SeedData.Seed(modelBuilder);
    }
}
