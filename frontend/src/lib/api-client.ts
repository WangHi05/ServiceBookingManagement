import axios, { AxiosError } from 'axios';
import { getToken, clearToken } from './auth-cookies';
import { isTokenExpired } from './jwt';
import type { ApiResponse } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// ---------- Request interceptor: tự đính JWT token ----------
apiClient.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    // Không có refresh token ở backend hiện tại (xem ghi chú trong README):
    // nếu token đã hết hạn, chủ động dọn cookie và để request đi tiếp KHÔNG kèm token,
    // backend sẽ trả 401 và response interceptor bên dưới sẽ tự logout + điều hướng /login.
    if (isTokenExpired(token)) {
      clearToken();
    } else {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }

  return config;
});

// ---------- Response interceptor: token hết hạn / bị từ chối -> tự logout ----------
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiResponse<unknown>>) => {
    if (error.response?.status === 401 && typeof window !== 'undefined') {
      clearToken();
      // Không dùng router.push ở đây vì interceptor nằm ngoài React tree;
      // điều hướng cứng đảm bảo mọi state cũ (đã gắn với user cũ) được reset sạch.
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

/**
 * Trích message lỗi thân thiện từ response lỗi của backend (ApiResponse<T>.message),
 * dùng thống nhất ở mọi nơi gọi API thay vì lặp lại logic try/catch mỗi chỗ.
 */
export function getErrorMessage(error: unknown, fallback = 'Đã xảy ra lỗi. Vui lòng thử lại.'): string {
  if (axios.isAxiosError(error)) {
    const apiMessage = (error.response?.data as ApiResponse<unknown> | undefined)?.message;
    if (apiMessage) return apiMessage;

    if (error.code === 'ERR_NETWORK') {
      return 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng hoặc backend đã chạy chưa.';
    }
  }

  return fallback;
}
