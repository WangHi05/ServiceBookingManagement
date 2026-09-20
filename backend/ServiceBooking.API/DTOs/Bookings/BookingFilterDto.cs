using ServiceBooking.API.DTOs.Common;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.DTOs.Bookings;

public class BookingFilterDto : PaginationQuery
{
    public BookingStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
