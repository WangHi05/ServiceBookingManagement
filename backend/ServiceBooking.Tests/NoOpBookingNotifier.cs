using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.Services;

namespace ServiceBooking.Tests;

/// <summary>
/// Test logic nghiệp vụ của BookingService không cần quan tâm tới việc SignalR có bắn sự kiện
/// hay không (đó là việc của tầng khác) - dùng no-op để không phải dựng cả 1 SignalR test server.
/// </summary>
public class NoOpBookingNotifier : IBookingNotifier
{
    public Task NotifyBookingChangedAsync(BookingDto booking) => Task.CompletedTask;
}
