using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Staffs;
using ServiceBooking.API.Services;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/staffs")]
[Authorize]
public class StaffsController : ControllerBase
{
    private readonly IStaffManagementService _staffManagementService;

    public StaffsController(IStaffManagementService staffManagementService)
    {
        _staffManagementService = staffManagementService;
    }

    /// <summary>Danh sách nhân viên. Customer dùng để chọn nhân viên khi đặt lịch.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool? isActive)
    {
        var result = await _staffManagementService.GetListAsync(isActive);
        return Ok(ApiResponse<List<StaffDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _staffManagementService.GetByIdAsync(id);
        return Ok(ApiResponse<StaffDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStaffDto dto)
    {
        var result = await _staffManagementService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<StaffDto>.Ok(result, "Tạo nhân viên thành công."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffDto dto)
    {
        var result = await _staffManagementService.UpdateAsync(id, dto);
        return Ok(ApiResponse<StaffDto>.Ok(result, "Cập nhật nhân viên thành công."));
    }

    /// <summary>
    /// Lịch làm việc của nhân viên. Có thể lọc theo khoảng ngày (fromDate/toDate),
    /// dùng để hiển thị lịch cho Customer chọn khung giờ.
    /// </summary>
    [HttpGet("{id:int}/schedules")]
    public async Task<IActionResult> GetSchedules(int id, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var result = await _staffManagementService.GetSchedulesAsync(id, fromDate, toDate);
        return Ok(ApiResponse<List<WorkScheduleDto>>.Ok(result));
    }

    [HttpPost("{id:int}/schedules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSchedule(int id, [FromBody] CreateWorkScheduleDto dto)
    {
        var result = await _staffManagementService.CreateScheduleAsync(id, dto);
        return CreatedAtAction(nameof(GetSchedules), new { id }, ApiResponse<WorkScheduleDto>.Ok(result, "Tạo lịch làm việc thành công."));
    }
}
