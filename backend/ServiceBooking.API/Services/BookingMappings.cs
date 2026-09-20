using System.Linq.Expressions;
using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

/// <summary>
/// Expression (KHÔNG phải method thường) để EF Core dịch được sang SQL và tự JOIN sang
/// Users/Services/Staffs. Viết thành method rồi gọi trong Select sẽ khiến EF đánh giá ở client
/// với navigation property chưa được load (= null) và gây NullReferenceException.
/// </summary>
public static class BookingMappings
{
    public static readonly Expression<Func<Booking, BookingDto>> ProjectToDto = b => new BookingDto
    {
        Id = b.Id,
        BookingCode = b.BookingCode,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer.FullName,
        ServiceId = b.ServiceId,
        ServiceName = b.Service.Name,
        StaffId = b.StaffId,
        StaffName = b.Staff.FullName,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        Status = b.Status.ToString(),
        CustomerNote = b.CustomerNote,
        CancellationReason = b.CancellationReason,
        CreatedAt = b.CreatedAt
    };
}
