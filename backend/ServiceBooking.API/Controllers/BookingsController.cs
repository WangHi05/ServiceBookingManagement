using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.Services;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Khung giờ trống cho 1 dịch vụ, 1 ngày, có thể lọc theo nhân viên.</summary>
    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] AvailableSlotsQueryDto query)
    {
        var result = await _bookingService.GetAvailableSlotsAsync(query);
        return Ok(ApiResponse<List<AvailableSlotDto>>.Ok(result));
    }

    /// <summary>Booking của chính người dùng đang đăng nhập, có lọc theo ngày/trạng thái + phân trang.</summary>
    [HttpGet("my-bookings")]
    public async Task<IActionResult> GetMyBookings([FromQuery] BookingFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.GetMyBookingsAsync(userId, filter);
        return Ok(ApiResponse<PagedResult<BookingDto>>.Ok(result));
    }

    /// <summary>Toàn bộ booking trong hệ thống (Admin), có lọc theo ngày/trạng thái + phân trang.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] BookingFilterDto filter)
    {
        var result = await _bookingService.GetAllAsync(filter);
        return Ok(ApiResponse<PagedResult<BookingDto>>.Ok(result));
    }

    /// <summary>Customer tạo booking mới. Chỉ chọn StartTime, backend tự tính EndTime và kiểm tra toàn bộ quy tắc nghiệp vụ.</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.CreateAsync(userId, dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<BookingDto>.Ok(result, "Đặt lịch thành công."));
    }

    /// <summary>Admin xác nhận / hoàn thành / hủy booking.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto)
    {
        var result = await _bookingService.UpdateStatusAsync(id, dto);
        return Ok(ApiResponse<BookingDto>.Ok(result, "Cập nhật trạng thái booking thành công."));
    }

    /// <summary>Hủy booking kèm lý do. Customer chỉ hủy được booking của chính mình; Admin hủy được mọi booking.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelBookingDto dto)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _bookingService.CancelAsync(id, userId, isAdmin, dto);
        return Ok(ApiResponse<BookingDto>.Ok(result, "Hủy booking thành công."));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (claim is null || !int.TryParse(claim, out var userId))
            throw ApiException.Unauthorized("Token không hợp lệ.");

        return userId;
    }
}
