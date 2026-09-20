import * as signalR from '@microsoft/signalr';
import { getToken } from './auth-cookies';

// API_URL dạng "http://localhost:5000/api" -> Hub nằm ở "http://localhost:5000/hubs/bookings"
// (Hub không nằm dưới tiền tố /api vì nó không phải REST endpoint).
const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000/api';
const HUB_URL = `${API_URL.replace(/\/api\/?$/, '')}/hubs/bookings`;

let connection: signalR.HubConnection | null = null;

/**
 * Dùng chung 1 connection (singleton) cho toàn app thay vì mỗi trang tự tạo riêng - tránh mở
 * nhiều WebSocket trùng lặp khi người dùng chuyển qua lại giữa /my-bookings và /admin/bookings.
 * Token được lấy qua accessTokenFactory (gọi lại mỗi lần cần reconnect), không hard-code 1 lần,
 * để tự dùng đúng token mới nhất nếu người dùng đăng nhập lại.
 */
export function getBookingHubConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();
  }

  return connection;
}

/**
 * Đảm bảo connection đang ở trạng thái Connected trước khi đăng ký lắng nghe sự kiện.
 * An toàn khi gọi nhiều lần (component khác cũng gọi) nhờ kiểm tra state trước khi start().
 */
export async function ensureBookingHubStarted(): Promise<void> {
  const conn = getBookingHubConnection();

  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start();
    } catch (err) {
      // Không throw ra ngoài: nếu realtime không kết nối được (vd backend chưa bật SignalR),
      // trang vẫn phải hoạt động bình thường ở chế độ "phải tự F5 mới thấy cập nhật mới".
      console.error('Không kết nối được SignalR:', err);
    }
  }
}
