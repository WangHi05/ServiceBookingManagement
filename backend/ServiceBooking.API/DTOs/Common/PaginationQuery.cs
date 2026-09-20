namespace ServiceBooking.API.DTOs.Common;

/// <summary>
/// Query parameters chung cho các endpoint có phân trang.
/// Clamp giá trị để tránh page size quá lớn gây tải toàn bộ bảng.
/// </summary>
public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            <= 0 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int SafePageNumber => PageNumber <= 0 ? 1 : PageNumber;
}
