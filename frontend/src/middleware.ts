import { NextRequest, NextResponse } from 'next/server';
import { jwtDecode } from 'jwt-decode';
import { TOKEN_COOKIE_NAME } from '@/lib/constants';

interface JwtPayload {
  role: 'Customer' | 'Admin';
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  exp: number;
}

const PUBLIC_ROUTES = ['/login'];
const ADMIN_ONLY_PREFIX = '/admin';

function readValidPayload(token: string | undefined): JwtPayload | null {
  if (!token) return null;

  try {
    const payload = jwtDecode<JwtPayload>(token);
    const nowInSeconds = Math.floor(Date.now() / 1000);
    if (payload.exp <= nowInSeconds) return null; // token hết hạn
    return payload;
  } catch {
    return null; // token malformed
  }
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const token = request.cookies.get(TOKEN_COOKIE_NAME)?.value;
  const payload = readValidPayload(token);

  const isPublicRoute = PUBLIC_ROUTES.includes(pathname);

  // 1. Trích xuất role an toàn từ token và chuyển thành chữ thường để dễ so sánh
  const rawRole = payload?.role || payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const userRole = rawRole?.toLowerCase();

  // Đã đăng nhập mà vào /login -> đẩy thẳng vào trang chính
  if (isPublicRoute && payload) {
    const homeRoute = userRole === 'admin' ? '/admin/services' : '/services';
    return NextResponse.redirect(new URL(homeRoute, request.url));
  }

  // Route cần đăng nhập mà không có token hợp lệ -> về /login
  if (!isPublicRoute && !payload) {
    const loginUrl = new URL('/login', request.url);
    loginUrl.searchParams.set('redirectTo', pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Route dành riêng cho Admin mà user không phải admin -> chặn
  if (pathname.startsWith(ADMIN_ONLY_PREFIX) && userRole !== 'admin') {
    return NextResponse.redirect(new URL('/services', request.url));
  }

  // Trang gốc: đã đăng nhập -> vào thẳng trang chính
  if (pathname === '/' && payload) {
    const homeRoute = userRole === 'admin' ? '/admin/services' : '/services';
    return NextResponse.redirect(new URL(homeRoute, request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Áp dụng middleware cho mọi route trừ static asset, file Next.js nội bộ, và favicon
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
