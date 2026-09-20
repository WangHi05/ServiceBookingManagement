import { apiClient } from './api-client';
import type {
  ApiResponse,
  AvailableSlot,
  Booking,
  BookingStatus,
  CreateBookingPayload,
  CreateWorkSchedulePayload,
  LoginResponse,
  PagedResult,
  Service,
  ServicePayload,
  Staff,
  StaffPayload,
  UpdateBookingStatusPayload,
  User,
  WorkSchedule,
} from './types';

// ================= Auth =================
export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<ApiResponse<LoginResponse>>('/auth/login', { email, password }),

  getMe: () => apiClient.get<ApiResponse<User>>('/auth/me'),
};

// ================= Services =================
export interface ServiceQuery {
  search?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export const servicesApi = {
  getList: (query: ServiceQuery) =>
    apiClient.get<ApiResponse<PagedResult<Service>>>('/services', { params: query }),

  create: (payload: ServicePayload) =>
    apiClient.post<ApiResponse<Service>>('/services', payload),

  update: (id: number, payload: ServicePayload) =>
    apiClient.put<ApiResponse<Service>>(`/services/${id}`, payload),
};

// ================= Staffs =================
export const staffsApi = {
  getList: (isActive?: boolean) =>
    apiClient.get<ApiResponse<Staff[]>>('/staffs', { params: { isActive } }),

  create: (payload: StaffPayload) => apiClient.post<ApiResponse<Staff>>('/staffs', payload),

  update: (id: number, payload: StaffPayload) =>
    apiClient.put<ApiResponse<Staff>>(`/staffs/${id}`, payload),

  getSchedules: (staffId: number, fromDate?: string, toDate?: string) =>
    apiClient.get<ApiResponse<WorkSchedule[]>>(`/staffs/${staffId}/schedules`, {
      params: { fromDate, toDate },
    }),

  createSchedule: (staffId: number, payload: CreateWorkSchedulePayload) =>
    apiClient.post<ApiResponse<WorkSchedule>>(`/staffs/${staffId}/schedules`, payload),
};

// ================= Bookings =================
export interface BookingFilter {
  status?: BookingStatus;
  fromDate?: string; // yyyy-MM-dd
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export const bookingsApi = {
  getAvailableSlots: (serviceId: number, date: string, staffId?: number) =>
    apiClient.get<ApiResponse<AvailableSlot[]>>('/bookings/available-slots', {
      params: { serviceId, date, staffId },
    }),

  getMyBookings: (filter: BookingFilter) =>
    apiClient.get<ApiResponse<PagedResult<Booking>>>('/bookings/my-bookings', { params: filter }),

  // Chỉ Admin được phép gọi (backend enforce bằng [Authorize(Roles = "Admin")])
  getAll: (filter: BookingFilter) =>
    apiClient.get<ApiResponse<PagedResult<Booking>>>('/bookings', { params: filter }),

  create: (payload: CreateBookingPayload) =>
    apiClient.post<ApiResponse<Booking>>('/bookings', payload),

  // Chỉ Admin: xác nhận (Confirmed) / hoàn thành (Completed) / hủy qua status (Cancelled)
  updateStatus: (bookingId: number, payload: UpdateBookingStatusPayload) =>
    apiClient.patch<ApiResponse<Booking>>(`/bookings/${bookingId}/status`, payload),

  cancel: (bookingId: number, reason: string) =>
    apiClient.post<ApiResponse<Booking>>(`/bookings/${bookingId}/cancel`, { reason }),
};
