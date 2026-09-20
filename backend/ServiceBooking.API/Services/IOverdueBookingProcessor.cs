namespace ServiceBooking.API.Services;

public interface IOverdueBookingProcessor
{
    /// <summary>Chạy định kỳ qua Hangfire - xử lý booking Pending quá hạn và Confirmed đã qua giờ hẹn.</summary>
    Task ProcessAsync();
}
