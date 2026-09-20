using ServiceBooking.API.DTOs.Staffs;

namespace ServiceBooking.API.Services;

public interface IStaffManagementService
{
    Task<List<StaffDto>> GetListAsync(bool? isActive);
    Task<StaffDto> GetByIdAsync(int id);
    Task<StaffDto> CreateAsync(CreateStaffDto dto);
    Task<StaffDto> UpdateAsync(int id, UpdateStaffDto dto);

    Task<List<WorkScheduleDto>> GetSchedulesAsync(int staffId, DateOnly? fromDate, DateOnly? toDate);
    Task<WorkScheduleDto> CreateScheduleAsync(int staffId, CreateWorkScheduleDto dto);
}
