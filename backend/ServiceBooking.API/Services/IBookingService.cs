using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Bookings;

namespace ServiceBooking.API.Services;

public interface IBookingService
{
    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotsQueryDto query);

    Task<BookingDto> CreateAsync(int customerId, CreateBookingDto dto);

    Task<PagedResult<BookingDto>> GetMyBookingsAsync(int customerId, BookingFilterDto filter);

    Task<PagedResult<BookingDto>> GetAllAsync(BookingFilterDto filter);

    Task<BookingDto> UpdateStatusAsync(int bookingId, UpdateBookingStatusDto dto);

    Task<BookingDto> CancelAsync(int bookingId, int actorUserId, bool isAdmin, CancelBookingDto dto);
}
