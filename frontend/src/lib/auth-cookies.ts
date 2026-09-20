import Cookies from 'js-cookie';
import { TOKEN_COOKIE_NAME } from './constants';

// Cookie KHÔNG dùng httpOnly (vì client JS cần tự đọc để đính vào header Authorization).
// Đây là đánh đổi hợp lý cho phạm vi demo: middleware Next.js vẫn đọc được cookie này
// để bảo vệ route ở tầng server, nhưng cookie có thể bị đọc bởi script khác trên trang
// (rủi ro XSS) — trong hệ thống thật nên chuyển sang httpOnly cookie + BFF proxy.
export { TOKEN_COOKIE_NAME };

const COOKIE_OPTIONS: Cookies.CookieAttributes = {
  expires: 1, // 1 ngày, khớp gần đúng với thời hạn token (ExpiresMinutes=120 ở backend);
  // vì token có thể hết hạn sớm hơn cookie, api-client vẫn luôn xử lý lỗi 401 để tự logout.
  sameSite: 'lax',
  path: '/',
};

export function saveToken(token: string) {
  Cookies.set(TOKEN_COOKIE_NAME, token, COOKIE_OPTIONS);
}

export function getToken(): string | undefined {
  return Cookies.get(TOKEN_COOKIE_NAME);
}

export function clearToken() {
  Cookies.remove(TOKEN_COOKIE_NAME, { path: '/' });
}
