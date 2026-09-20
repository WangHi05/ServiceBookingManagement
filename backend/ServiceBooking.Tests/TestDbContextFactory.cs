using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Data;

namespace ServiceBooking.Tests;

/// <summary>
/// Tạo 1 ApplicationDbContext riêng biệt, cô lập hoàn toàn cho mỗi test, dùng SQLite in-memory.
///
/// KHÔNG dùng EF Core InMemory provider (Microsoft.EntityFrameworkCore.InMemory) vì provider đó
/// không hỗ trợ transaction thật (Database.BeginTransactionAsync sẽ ném lỗi), trong khi
/// BookingService.CreateAsync bắt buộc dùng transaction (IsolationLevel.Serializable) để chống
/// race condition khi đặt trùng lịch — đây chính là phần logic quan trọng nhất cần test.
/// SQLite in-memory hỗ trợ transaction thật nên mô phỏng đúng hành vi chạy với SQL Server thật.
/// </summary>
public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext Context { get; }

    public TestDbContextFactory()
    {
        // Phải giữ connection mở suốt vòng đời DbContext: SQLite in-memory xóa sạch dữ liệu
        // ngay khi connection cuối cùng đóng lại.
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);

        // EnsureCreated dựng schema trực tiếp từ model hiện tại (bao gồm cả HasData seed trong
        // SeedData.cs) mà không cần thư mục Migrations - mỗi test có sẵn đúng bộ dữ liệu mẫu
        // giống hệt khi chạy `dotnet ef database update` (1 Admin, 2 Customer, 2 Staff, 5 Service...).
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
