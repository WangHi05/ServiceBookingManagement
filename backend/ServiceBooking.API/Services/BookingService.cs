using System.Data;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Common;
using ServiceBooking.API.Data;
using ServiceBooking.API.DTOs.Bookings;
using ServiceBooking.API.DTOs.Common;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

public class BookingService : IBookingService
{
    // Bước nhảy khi sinh danh sách khung giờ trống. Có thể đưa ra appsettings nếu cần cấu hình.
    private const int SlotIntervalMinutes = 30;

    private static readonly Dictionary<BookingStatus, BookingStatus[]> AllowedStatusTransitions = new()
    {
        [BookingStatus.Pending] = new[] { BookingStatus.Confirmed, BookingStatus.Cancelled },
        [BookingStatus.Confirmed] = new[] { BookingStatus.Completed, BookingStatus.Cancelled },
        [BookingStatus.Completed] = Array.Empty<BookingStatus>(),
        [BookingStatus.Cancelled] = Array.Empty<BookingStatus>()
    };

    private readonly ApplicationDbContext _context;
    private readonly IBookingNotifier _notifier;

    public BookingService(ApplicationDbContext context, IBookingNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    // ================= AVAILABLE SLOTS =================

    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotsQueryDto query)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == query.ServiceId)
            ?? throw ApiException.NotFound($"Không tìm thấy dịch vụ có Id = {query.ServiceId}.");

        if (!service.IsActive)
            throw ApiException.BadRequest("Dịch vụ này hiện đang bị khóa, không thể đặt lịch.");

        var staffQuery = _context.Staffs.AsNoTracking().Where(s => s.IsActive);
        if (query.StaffId.HasValue)
            staffQuery = staffQuery.Where(s => s.Id == query.StaffId.Value);

        var staffList = await staffQuery.ToListAsync();

        if (query.StaffId.HasValue && staffList.Count == 0)
            throw ApiException.NotFound("Không tìm thấy nhân viên hoặc nhân viên đang bị khóa.");

        if (staffList.Count == 0)
            return new List<AvailableSlotDto>();

        var staffIds = staffList.Select(s => s.Id).ToList();

        var schedules = await _context.WorkSchedules.AsNoTracking()
            .Where(ws => staffIds.Contains(ws.StaffId) && ws.WorkDate == query.Date)
            .ToListAsync();

        if (schedules.Count == 0)
            return new List<AvailableSlotDto>();

        var dayStart = query.Date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        // Lấy trước toàn bộ booking chưa hủy trong ngày của các staff liên quan, tránh N+1 query
        var existingBookings = await _context.Bookings.AsNoTracking()
            .Where(b => staffIds.Contains(b.StaffId)
                        && b.Status != BookingStatus.Cancelled
                        && b.StartTime < dayEnd && b.EndTime > dayStart)
            .Select(b => new { b.StaffId, b.StartTime, b.EndTime })
            .ToListAsync();

        var result = new List<AvailableSlotDto>();
        var now = DateTime.Now;

        foreach (var schedule in schedules)
        {
            var staff = staffList.First(s => s.Id == schedule.StaffId);
            var scheduleStart = schedule.WorkDate.ToDateTime(schedule.StartTime);
            var scheduleEnd = schedule.WorkDate.ToDateTime(schedule.EndTime);

            for (var slotStart = scheduleStart;
                 slotStart.AddMinutes(service.DurationMinutes) <= scheduleEnd;
                 slotStart = slotStart.AddMinutes(SlotIntervalMinutes))
            {
                var slotEnd = slotStart.AddMinutes(service.DurationMinutes);

                if (slotStart <= now) continue; // không gợi ý slot trong quá khứ

                var isOverlapping = existingBookings.Any(b =>
                    b.StaffId == schedule.StaffId && slotStart < b.EndTime && slotEnd > b.StartTime);

                if (isOverlapping) continue;

                result.Add(new AvailableSlotDto
                {
                    StaffId = staff.Id,
                    StaffName = staff.FullName,
                    StartTime = slotStart,
                    EndTime = slotEnd
                });
            }
        }

        return result.OrderBy(s => s.StartTime).ThenBy(s => s.StaffId).ToList();
    }

    // ================= CREATE BOOKING =================

    public async Task<BookingDto> CreateAsync(int customerId, CreateBookingDto dto)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.ServiceId)
            ?? throw ApiException.NotFound("Không tìm thấy dịch vụ.");

        if (!service.IsActive)
            throw ApiException.BadRequest("Dịch vụ đang bị khóa, không thể đặt lịch.");

        var staff = await _context.Staffs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.StaffId)
            ?? throw ApiException.NotFound("Không tìm thấy nhân viên.");

        if (!staff.IsActive)
            throw ApiException.BadRequest("Nhân viên đang bị khóa, không thể đặt lịch.");

        // 5.2 Không đặt lịch trong quá khứ
        if (dto.StartTime <= DateTime.Now)
            throw ApiException.BadRequest("Không thể đặt lịch trong quá khứ.");

        // 5.1 Tự tính EndTime
        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        // 5.2 Booking phải nằm hoàn toàn trong giờ làm việc (không cho phép vắt qua nửa đêm)
        if (endTime.Date != dto.StartTime.Date)
            throw ApiException.BadRequest("Khung giờ đặt lịch nằm ngoài giờ làm việc của nhân viên.");

        var workDate = DateOnly.FromDateTime(dto.StartTime);
        var startTimeOfDay = TimeOnly.FromDateTime(dto.StartTime);
        var endTimeOfDay = TimeOnly.FromDateTime(endTime);

        var fitsInSchedule = await _context.WorkSchedules.AsNoTracking()
            .Where(ws => ws.StaffId == dto.StaffId && ws.WorkDate == workDate)
            .AnyAsync(ws => startTimeOfDay >= ws.StartTime && endTimeOfDay <= ws.EndTime);

        if (!fitsInSchedule)
            throw ApiException.BadRequest("Khung giờ đặt lịch nằm ngoài giờ làm việc của nhân viên.");

        // 5.3 Chống trùng lịch. Dùng transaction + Serializable isolation level để giảm thiểu
        // race condition khi 2 request đặt cùng khung giờ gửi lên gần như đồng thời (mục 12).
        int newBookingId;
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var hasConflict = await _context.Bookings
                .Where(b => b.StaffId == dto.StaffId && b.Status != BookingStatus.Cancelled)
                .AnyAsync(b => dto.StartTime < b.EndTime && endTime > b.StartTime);

            if (hasConflict)
                throw ApiException.Conflict("Khung giờ này đã có người đặt. Vui lòng chọn khung giờ khác.");

            var booking = new Booking
            {
                CustomerId = customerId,
                ServiceId = dto.ServiceId,
                StaffId = dto.StaffId,
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = BookingStatus.Pending,
                CustomerNote = string.IsNullOrWhiteSpace(dto.CustomerNote) ? null : dto.CustomerNote.Trim(),
                // Placeholder tạm thời phải DUY NHẤT vì BookingCode có unique index:
                // nếu dùng hằng số cố định, 2 request đồng thời sẽ đụng unique constraint.
                // Giá trị này được thay bằng BK{Id:D6} ngay sau khi có Id.
                BookingCode = $"TMP-{Guid.NewGuid():N}"[..20]
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            booking.BookingCode = $"BK{booking.Id:D6}";
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            newBookingId = booking.Id;
        }
        catch
        {
            // Chỉ rollback khi transaction chưa commit. Nếu commit đã xong mà vẫn rollback,
            // SqlTransaction sẽ ném InvalidOperationException ("This SqlTransaction has completed")
            // và che mất exception gốc.
            await transaction.RollbackAsync();
            throw;
        }

        // Map ra DTO SAU khi transaction kết thúc: nếu bước này lỗi thì booking vẫn đã được
        // lưu hợp lệ, và lỗi sẽ không kéo theo thao tác rollback không hợp lệ.
        var result = await MapToDtoAsync(newBookingId);
        await _notifier.NotifyBookingChangedAsync(result);
        return result;
    }

    // ================= LIST / FILTER =================

    public async Task<PagedResult<BookingDto>> GetMyBookingsAsync(int customerId, BookingFilterDto filter)
    {
        var query = _context.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId);
        query = ApplyFilter(query, filter);
        return await PaginateAsync(query, filter);
    }

    public async Task<PagedResult<BookingDto>> GetAllAsync(BookingFilterDto filter)
    {
        var query = _context.Bookings.AsNoTracking().AsQueryable();
        query = ApplyFilter(query, filter);
        return await PaginateAsync(query, filter);
    }

    private static IQueryable<Booking> ApplyFilter(IQueryable<Booking> query, BookingFilterDto filter)
    {
        if (filter.Status.HasValue)
            query = query.Where(b => b.Status == filter.Status.Value);

        if (filter.FromDate.HasValue)
        {
            var from = filter.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(b => b.StartTime >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var to = filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(b => b.StartTime <= to);
        }

        return query;
    }

    private async Task<PagedResult<BookingDto>> PaginateAsync(IQueryable<Booking> query, PaginationQuery pagination)
    {
        var totalCount = await query.CountAsync();

        // Phân trang tại database (Skip/Take -> OFFSET/FETCH), không tải hết bảng rồi mới cắt
        var items = await query
            .OrderByDescending(b => b.StartTime)
            .Skip((pagination.SafePageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(BookingMappings.ProjectToDto)
            .ToListAsync();

        return new PagedResult<BookingDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.SafePageNumber,
            PageSize = pagination.PageSize
        };
    }

    // ================= STATUS UPDATE (ADMIN) =================

    public async Task<BookingDto> UpdateStatusAsync(int bookingId, UpdateBookingStatusDto dto)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw ApiException.NotFound("Không tìm thấy booking.");

        ValidateStatusTransition(booking.Status, dto.Status);

        if (dto.Status == BookingStatus.Cancelled)
        {
            ValidateCancellable(booking);

            if (string.IsNullOrWhiteSpace(dto.CancellationReason))
                throw ApiException.BadRequest("Lý do hủy là bắt buộc khi chuyển trạng thái sang Cancelled.");

            booking.CancellationReason = dto.CancellationReason.Trim();
        }

        booking.Status = dto.Status;
        await _context.SaveChangesAsync();

        var result = await MapToDtoAsync(booking.Id);
        await _notifier.NotifyBookingChangedAsync(result);
        return result;
    }

    // ================= CANCEL (CUSTOMER hoặc ADMIN) =================

    public async Task<BookingDto> CancelAsync(int bookingId, int actorUserId, bool isAdmin, CancelBookingDto dto)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw ApiException.NotFound("Không tìm thấy booking.");

        // 5.4 Customer chỉ được hủy booking của chính mình. Kiểm tra độc lập với frontend.
        if (!isAdmin && booking.CustomerId != actorUserId)
            throw ApiException.Forbidden("Bạn không có quyền hủy booking này.");

        ValidateCancellable(booking);

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = dto.Reason.Trim();

        await _context.SaveChangesAsync();

        var result = await MapToDtoAsync(booking.Id);
        await _notifier.NotifyBookingChangedAsync(result);
        return result;
    }

    // ================= HELPERS =================

    private static void ValidateCancellable(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled)
            throw ApiException.BadRequest("Booking này đã bị hủy trước đó.");

        if (booking.Status == BookingStatus.Completed)
            throw ApiException.BadRequest("Không thể hủy booking đã hoàn thành.");

        if (booking.StartTime <= DateTime.Now)
            throw ApiException.BadRequest("Không thể hủy booking đã bắt đầu.");
    }

    private static void ValidateStatusTransition(BookingStatus current, BookingStatus next)
    {
        if (current == next)
            throw ApiException.BadRequest($"Booking đang ở trạng thái {current} rồi.");

        if (!AllowedStatusTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw ApiException.BadRequest($"Không thể chuyển trạng thái từ {current} sang {next}.");
    }

    private async Task<BookingDto> MapToDtoAsync(int bookingId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(BookingMappings.ProjectToDto)
            .FirstAsync();
    }
}
