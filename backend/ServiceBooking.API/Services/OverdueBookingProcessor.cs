using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Data;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

/// <summary>
/// Chạy định kỳ (đăng ký trong Program.cs qua RecurringJob.AddOrUpdate, mặc định mỗi 5 phút).
/// Xử lý 2 tình huống "booking quá hạn" mà không ai chủ động cập nhật trạng thái:
///
/// 1) Booking còn Pending (Admin chưa xác nhận) mà giờ hẹn đã trôi qua -> không còn ý nghĩa giữ
///    chỗ nữa, tự động Hủy để giải phóng khung giờ và minh bạch cho khách hàng.
/// 2) Booking đã Confirmed nhưng Admin quên bấm "Hoàn thành" dù giờ hẹn đã kết thúc -> tự động
///    đánh dấu Hoàn thành (giả định dịch vụ đã diễn ra đúng như lịch đã xác nhận).
///
/// Mỗi lần xử lý xong đều bắn sự kiện SignalR để UI đang mở (nếu có) tự cập nhật ngay, không
/// cần đợi người dùng F5 mới thấy trạng thái mới.
/// </summary>
public class OverdueBookingProcessor : IOverdueBookingProcessor
{
    private const string AutoCancelReason = "Hệ thống tự động hủy: quá giờ hẹn mà chưa được xác nhận.";

    private readonly ApplicationDbContext _context;
    private readonly IBookingNotifier _notifier;
    private readonly ILogger<OverdueBookingProcessor> _logger;

    public OverdueBookingProcessor(ApplicationDbContext context, IBookingNotifier notifier, ILogger<OverdueBookingProcessor> logger)
    {
        _context = context;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        var now = DateTime.Now;

        var cancelledIds = await AutoCancelUnconfirmedOverdueBookingsAsync(now);
        var completedIds = await AutoCompleteConfirmedFinishedBookingsAsync(now);

        foreach (var id in cancelledIds.Concat(completedIds))
        {
            var dto = await _context.Bookings.AsNoTracking()
                .Where(b => b.Id == id)
                .Select(BookingMappings.ProjectToDto)
                .FirstAsync();

            await _notifier.NotifyBookingChangedAsync(dto);
        }
    }

    private async Task<List<int>> AutoCancelUnconfirmedOverdueBookingsAsync(DateTime now)
    {
        var overdue = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.StartTime <= now)
            .ToListAsync();

        foreach (var booking in overdue)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = AutoCancelReason;
        }

        if (overdue.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Hangfire: tự động hủy {Count} booking Pending quá hạn.", overdue.Count);
        }

        return overdue.Select(b => b.Id).ToList();
    }

    private async Task<List<int>> AutoCompleteConfirmedFinishedBookingsAsync(DateTime now)
    {
        var finished = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.EndTime <= now)
            .ToListAsync();

        foreach (var booking in finished)
        {
            booking.Status = BookingStatus.Completed;
        }

        if (finished.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Hangfire: tự động hoàn thành {Count} booking Confirmed đã qua giờ hẹn.", finished.Count);
        }

        return finished.Select(b => b.Id).ToList();
    }
}
