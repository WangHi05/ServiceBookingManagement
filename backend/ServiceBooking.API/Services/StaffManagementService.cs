using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Common;
using ServiceBooking.API.Data;
using ServiceBooking.API.DTOs.Staffs;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

public class StaffManagementService : IStaffManagementService
{
    private readonly ApplicationDbContext _context;

    public StaffManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StaffDto>> GetListAsync(bool? isActive)
    {
        var query = _context.Staffs.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(s => s.FullName)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<StaffDto> GetByIdAsync(int id)
    {
        var staff = await _context.Staffs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id)
            ?? throw ApiException.NotFound($"Không tìm thấy nhân viên có Id = {id}.");

        return MapToDto(staff);
    }

    public async Task<StaffDto> CreateAsync(CreateStaffDto dto)
    {
        var emailExists = await _context.Staffs.AnyAsync(s => s.Email == dto.Email);
        if (emailExists)
            throw ApiException.Conflict("Email nhân viên đã tồn tại.");

        var staff = new Staff
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            IsActive = true
        };

        _context.Staffs.Add(staff);
        await _context.SaveChangesAsync();

        return MapToDto(staff);
    }

    public async Task<StaffDto> UpdateAsync(int id, UpdateStaffDto dto)
    {
        var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw ApiException.NotFound($"Không tìm thấy nhân viên có Id = {id}.");

        var emailTaken = await _context.Staffs.AnyAsync(s => s.Email == dto.Email && s.Id != id);
        if (emailTaken)
            throw ApiException.Conflict("Email nhân viên đã tồn tại.");

        staff.FullName = dto.FullName.Trim();
        staff.Email = dto.Email.Trim();
        staff.IsActive = dto.IsActive; // khóa/mở khóa nhân viên

        await _context.SaveChangesAsync();

        return MapToDto(staff);
    }

    public async Task<List<WorkScheduleDto>> GetSchedulesAsync(int staffId, DateOnly? fromDate, DateOnly? toDate)
    {
        var staffExists = await _context.Staffs.AnyAsync(s => s.Id == staffId);
        if (!staffExists)
            throw ApiException.NotFound($"Không tìm thấy nhân viên có Id = {staffId}.");

        var query = _context.WorkSchedules.AsNoTracking().Where(ws => ws.StaffId == staffId);

        if (fromDate.HasValue)
            query = query.Where(ws => ws.WorkDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ws => ws.WorkDate <= toDate.Value);

        return await query
            .OrderBy(ws => ws.WorkDate).ThenBy(ws => ws.StartTime)
            .Select(ws => new WorkScheduleDto
            {
                Id = ws.Id,
                StaffId = ws.StaffId,
                WorkDate = ws.WorkDate,
                StartTime = ws.StartTime,
                EndTime = ws.EndTime
            })
            .ToListAsync();
    }

    public async Task<WorkScheduleDto> CreateScheduleAsync(int staffId, CreateWorkScheduleDto dto)
    {
        var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw ApiException.NotFound($"Không tìm thấy nhân viên có Id = {staffId}.");

        if (dto.StartTime >= dto.EndTime)
            throw ApiException.BadRequest("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");

        // Không tạo hai ca làm việc bị trùng cho cùng nhân viên trong cùng ngày
        var isOverlapping = await _context.WorkSchedules
            .Where(ws => ws.StaffId == staffId && ws.WorkDate == dto.WorkDate)
            .AnyAsync(ws => dto.StartTime < ws.EndTime && dto.EndTime > ws.StartTime);

        if (isOverlapping)
            throw ApiException.Conflict("Ca làm việc bị trùng với ca đã tồn tại của nhân viên này.");

        var schedule = new Models.WorkSchedule
        {
            StaffId = staffId,
            WorkDate = dto.WorkDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        _context.WorkSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return new WorkScheduleDto
        {
            Id = schedule.Id,
            StaffId = schedule.StaffId,
            WorkDate = schedule.WorkDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime
        };
    }

    private static StaffDto MapToDto(Staff s) => new()
    {
        Id = s.Id,
        FullName = s.FullName,
        Email = s.Email,
        IsActive = s.IsActive
    };
}
