using System.Net;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Staffs;
using ServiceBooking.API.Services;
using Xunit;

namespace ServiceBooking.Tests;

public class StaffManagementServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly StaffManagementService _sut;

    public StaffManagementServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new StaffManagementService(_factory.Context);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateScheduleAsync_ThrowsBadRequest_WhenStartTimeAfterEndTime()
    {
        var dto = new CreateWorkScheduleDto
        {
            WorkDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(8, 0)
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateScheduleAsync(staffId: 1, dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateScheduleAsync_Throws409Conflict_WhenOverlappingExistingShift()
    {
        var workDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5));

        await _sut.CreateScheduleAsync(1, new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0)
        });

        var overlapping = new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(11, 0), // chồng lấn 1 tiếng với ca 08:00-12:00
            EndTime = new TimeOnly(15, 0)
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateScheduleAsync(1, overlapping));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task CreateScheduleAsync_Succeeds_WhenShiftsAreAdjacentNotOverlapping()
    {
        var workDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5));

        await _sut.CreateScheduleAsync(1, new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0)
        });

        // Ca thứ 2 bắt đầu đúng lúc ca 1 kết thúc -> không được coi là trùng
        var result = await _sut.CreateScheduleAsync(1, new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(17, 0)
        });

        Assert.Equal(new TimeOnly(12, 0), result.StartTime);
    }

    [Fact]
    public async Task CreateScheduleAsync_AllowsSameTimeSlot_ForDifferentStaff()
    {
        var workDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5));

        await _sut.CreateScheduleAsync(1, new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0)
        });

        // Cùng khung giờ nhưng khác nhân viên (staffId=2) -> không có gì ngăn cản
        var result = await _sut.CreateScheduleAsync(2, new CreateWorkScheduleDto
        {
            WorkDate = workDate,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0)
        });

        Assert.Equal(2, result.StaffId);
    }
}
