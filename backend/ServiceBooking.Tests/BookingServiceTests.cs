using System.Net;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.Models;
using ServiceBooking.API.Services;
using Xunit;

namespace ServiceBooking.Tests;

/// <summary>
/// Test dữ liệu dùng ServiceId=1 ("Cắt tóc nam", 30 phút) và StaffId=1 (staff1) có sẵn từ SeedData.
/// Mọi ngày giờ dùng trong test đều tính TƯƠNG ĐỐI theo DateTime.Now (vd: "10 ngày sau hôm nay")
/// thay vì ngày cố định — để bộ test không bao giờ bị lỗi "hết hạn" khi chạy vào một ngày khác
/// (khác với dữ liệu WorkSchedule/Booking tĩnh trong SeedData vốn chỉ dùng để demo UI).
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly BookingService _sut; // system under test

    public BookingServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new BookingService(_factory.Context, new NoOpBookingNotifier());
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>Tạo sẵn 1 ca làm việc cho staff vào ngày "daysFromNow" ngày sau hôm nay, trả về mốc 00:00 của ngày đó.</summary>
    private async Task<DateTime> SeedFutureWorkScheduleAsync(int staffId, int daysFromNow, TimeOnly start, TimeOnly end)
    {
        var workDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysFromNow));
        _factory.Context.WorkSchedules.Add(new WorkSchedule
        {
            StaffId = staffId,
            WorkDate = workDate,
            StartTime = start,
            EndTime = end
        });
        await _factory.Context.SaveChangesAsync();
        return workDate.ToDateTime(TimeOnly.MinValue);
    }

    // ---------- TC1: Không đặt lịch quá khứ ----------
    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenStartTimeInPast()
    {
        var dto = new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = DateTime.Now.AddDays(-1) };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(customerId: 2, dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("quá khứ", ex.Message);
    }

    // ---------- TC2: Không đặt lịch ngoài giờ làm việc ----------
    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenOutsideWorkingHours()
    {
        var day = await SeedFutureWorkScheduleAsync(staffId: 1, daysFromNow: 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var dto = new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(18) }; // 18:00, ngoài ca

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(2, dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("ngoài giờ làm việc", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenNoWorkScheduleExistsForThatDate()
    {
        // Không seed WorkSchedule nào cho ngày này -> phải bị chặn dù giờ hợp lý (09:00)
        var dto = new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = DateTime.Now.Date.AddDays(15).AddHours(9) };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(2, dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    // ---------- Tự tính EndTime ----------
    [Fact]
    public async Task CreateAsync_Succeeds_AndAutoCalculatesEndTime()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var result = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        Assert.Equal("Pending", result.Status);
        Assert.Equal(day.AddHours(9).AddMinutes(30), result.EndTime); // Service Id=1 = 30 phút
        Assert.StartsWith("BK", result.BookingCode);
    }

    // ---------- TC3: Không đặt hai booking trùng giờ (409) ----------
    [Fact]
    public async Task CreateAsync_Throws409Conflict_WhenOverlappingExistingBooking()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) }); // 09:00-09:30

        var conflicting = new CreateBookingDto
        {
            ServiceId = 1,
            StaffId = 1,
            StartTime = day.AddHours(9).AddMinutes(15) // 09:15-09:45, chồng lấn 09:00-09:30
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(3, conflicting));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenNewBookingStartsExactlyWhenExistingOneEnds()
    {
        // Kiểm chứng đúng biên công thức bắt buộc: NewStart == ExistingEnd -> KHÔNG coi là trùng
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) }); // 09:00-09:30

        var adjacent = new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9).AddMinutes(30) };

        var result = await _sut.CreateAsync(3, adjacent); // không được ném lỗi

        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_DoesNotConflict_WithAnAlreadyCancelledBooking()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var first = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });
        await _sut.CancelAsync(first.Id, actorUserId: 2, isAdmin: false, new CancelBookingDto { Reason = "Đổi ý" });

        // Đặt lại đúng khung giờ vừa hủy -> phải thành công vì booking cũ đã Cancelled
        var result = await _sut.CreateAsync(3, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenServiceIsInactive()
    {
        var service = await _factory.Context.Services.FindAsync(5); // "Spa da mặt" - active theo seed
        service!.IsActive = false;
        await _factory.Context.SaveChangesAsync();

        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 5, StaffId = 1, StartTime = day.AddHours(9) }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("khóa", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenStaffIsInactive()
    {
        var staff = await _factory.Context.Staffs.FindAsync(2);
        staff!.IsActive = false;
        await _factory.Context.SaveChangesAsync();

        var day = await SeedFutureWorkScheduleAsync(2, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 2, StartTime = day.AddHours(9) }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("khóa", ex.Message);
    }

    // ---------- TC4: Customer không hủy được booking của người khác ----------
    [Fact]
    public async Task CancelAsync_ThrowsForbidden_WhenActorIsNotOwnerAndNotAdmin()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(customerId: 2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.CancelAsync(booking.Id, actorUserId: 3, isAdmin: false, new CancelBookingDto { Reason = "Test" }));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task CancelAsync_Succeeds_WhenAdminCancelsAnyonesBooking()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        var result = await _sut.CancelAsync(booking.Id, actorUserId: 1 /* Admin */, isAdmin: true, new CancelBookingDto { Reason = "Admin hủy hộ" });

        Assert.Equal("Cancelled", result.Status);
    }

    // ---------- TC6: Không hủy được booking đã hoàn thành ----------
    [Fact]
    public async Task CancelAsync_ThrowsBadRequest_WhenBookingAlreadyCompleted()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        await _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Confirmed });
        await _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Completed });

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.CancelAsync(booking.Id, actorUserId: 2, isAdmin: false, new CancelBookingDto { Reason = "Đổi ý" }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("hoàn thành", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_ThrowsBadRequest_WhenBookingAlreadyStarted()
    {
        // Tạo trực tiếp vào DB (bỏ qua validate "không đặt quá khứ" của CreateAsync) để mô phỏng
        // đúng tình huống "booking đã tồn tại từ trước và giờ bắt đầu vừa trôi qua".
        var pastStartBooking = new Booking
        {
            BookingCode = "BKTEST01",
            CustomerId = 2,
            ServiceId = 1,
            StaffId = 1,
            StartTime = DateTime.Now.AddMinutes(-5),
            EndTime = DateTime.Now.AddMinutes(25),
            Status = BookingStatus.Pending
        };
        _factory.Context.Bookings.Add(pastStartBooking);
        await _factory.Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.CancelAsync(pastStartBooking.Id, actorUserId: 2, isAdmin: false, new CancelBookingDto { Reason = "Trễ" }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("đã bắt đầu", ex.Message);
    }

    // ---------- Chuyển trạng thái hợp lệ / không hợp lệ ----------
    [Fact]
    public async Task UpdateStatusAsync_ThrowsBadRequest_OnInvalidTransition_PendingToCompleted()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        // Pending -> Completed (bỏ qua Confirmed) phải bị chặn
        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Completed }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_Succeeds_ThroughFullValidLifecycle()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        var confirmed = await _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Confirmed });
        Assert.Equal("Confirmed", confirmed.Status);

        var completed = await _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Completed });
        Assert.Equal("Completed", completed.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ThrowsBadRequest_WhenCancellingWithoutReason()
    {
        var day = await SeedFutureWorkScheduleAsync(1, 10, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var booking = await _sut.CreateAsync(2, new CreateBookingDto { ServiceId = 1, StaffId = 1, StartTime = day.AddHours(9) });

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateStatusAsync(booking.Id, new UpdateBookingStatusDto { Status = BookingStatus.Cancelled, CancellationReason = null }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }
}
