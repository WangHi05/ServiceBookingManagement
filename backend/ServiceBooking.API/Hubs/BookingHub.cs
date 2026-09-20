using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ServiceBooking.API.Hubs;

/// <summary>
/// Hub SignalR duy nhất của hệ thống, chỉ dùng để đẩy sự kiện "BookingChanged" khi 1 booking
/// được tạo/đổi trạng thái/hủy - phía client (Customer lẫn Admin) lắng nghe để cập nhật UI mà
/// không cần bấm F5 hay polling định kỳ.
///
/// Không có method nào cho client gọi lên server (server -> client one-way), nên toàn bộ logic
/// chỉ nằm ở việc join đúng group lúc kết nối.
/// </summary>
[Authorize]
public class BookingHub : Hub
{
    public const string AdminsGroup = "Admins";

    public static string CustomerGroup(int customerId) => $"Customer-{customerId}";

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (int.TryParse(userIdClaim, out var userId))
        {
            // Mọi user (Customer lẫn Admin) đều join group riêng của chính mình - dùng khi
            // Admin cũng muốn theo dõi các booking do chính họ tạo hộ (nếu sau này có tính năng đó).
            await Groups.AddToGroupAsync(Context.ConnectionId, CustomerGroup(userId));
        }

        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }
}
