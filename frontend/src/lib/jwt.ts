import { jwtDecode } from 'jwt-decode';
import type { UserRole } from './types';

// Claim "sub"/"email"/"role" do backend TokenService.cs sinh ra (xem ServiceBooking.API).
// LƯU Ý: decode ở đây KHÔNG verify chữ ký — chỉ dùng để hiển thị UI (ví dụ ẩn/hiện menu
// theo role, hoặc kiểm tra hết hạn để chủ động logout sớm). Mọi kiểm tra quyền thật sự
// vẫn nằm ở backend ([Authorize(Roles=...)]) — FE không được tin tưởng tuyệt đối vào token.
interface JwtPayload {
  sub: string;
  email: string;
  role: UserRole;
  exp: number; // unix timestamp (giây)
}

export function decodeToken(token: string): JwtPayload | null {
  try {
    return jwtDecode<JwtPayload>(token);
  } catch {
    return null;
  }
}

export function isTokenExpired(token: string): boolean {
  const payload = decodeToken(token);
  if (!payload) return true;

  const nowInSeconds = Math.floor(Date.now() / 1000);
  return payload.exp <= nowInSeconds;
}
