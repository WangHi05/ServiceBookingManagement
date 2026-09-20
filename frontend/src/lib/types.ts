// Toàn bộ type ở đây khớp 1-1 với các DTO trong backend (ServiceBooking.API/DTOs/**)
// để tránh lệch field khi FE/BE cùng phát triển song song.

export type UserRole = 'Customer' | 'Admin';

export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ---------- Auth ----------
export interface User {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

// ---------- Services ----------
export interface Service {
  id: number;
  name: string;
  description: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

// ---------- Staff ----------
export interface Staff {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface WorkSchedule {
  id: number;
  staffId: number;
  workDate: string; // yyyy-MM-dd
  startTime: string; // HH:mm:ss
  endTime: string;
}

// ---------- Bookings ----------
export type BookingStatus = 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled';

export interface Booking {
  id: number;
  bookingCode: string;
  customerId: number;
  customerName: string;
  serviceId: number;
  serviceName: string;
  staffId: number;
  staffName: string;
  startTime: string; // ISO datetime string
  endTime: string;
  status: BookingStatus;
  customerNote: string | null;
  cancellationReason: string | null;
  createdAt: string;
}

export interface AvailableSlot {
  staffId: number;
  staffName: string;
  startTime: string;
  endTime: string;
}

export interface CreateBookingPayload {
  serviceId: number;
  staffId: number;
  startTime: string;
  customerNote?: string;
}

// ---------- Admin payloads ----------
export interface ServicePayload {
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive?: boolean; // chỉ dùng khi Update
}

export interface StaffPayload {
  fullName: string;
  email: string;
  isActive?: boolean; // chỉ dùng khi Update
}

export interface CreateWorkSchedulePayload {
  workDate: string; // yyyy-MM-dd
  startTime: string; // HH:mm
  endTime: string;
}

export interface UpdateBookingStatusPayload {
  status: BookingStatus;
  cancellationReason?: string;
}
