using Microsoft.AspNetCore.SignalR;
using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.Hubs;

namespace ServiceBooking.API.Services;

public interface IBookingNotifier
{
    /// <summary>Đẩy sự kiện "BookingChanged" tới Admin (mọi booking) và đúng khách hàng sở hữu booking đó.</summary>
    Task NotifyBookingChangedAsync(BookingDto booking);
}

/// <summary>
/// Bọc IHubContext&lt;BookingHub&gt; thành 1 service riêng (thay vì inject thẳng IHubContext vào
/// BookingService) để BookingService không phụ thuộc trực tiếp vào SignalR - dễ thay đổi cơ chế
/// realtime sau này (hoặc tắt hẳn) mà không phải sửa logic nghiệp vụ.
/// </summary>
public class BookingNotifier : IBookingNotifier
{
    private readonly IHubContext<BookingHub> _hubContext;

    public BookingNotifier(IHubContext<BookingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBookingChangedAsync(BookingDto booking)
    {
        await _hubContext.Clients.Group(BookingHub.AdminsGroup).SendAsync("BookingChanged", booking);
        await _hubContext.Clients.Group(BookingHub.CustomerGroup(booking.CustomerId)).SendAsync("BookingChanged", booking);
    }
}
