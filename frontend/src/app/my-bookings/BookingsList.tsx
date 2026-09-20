'use client';

import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { bookingsApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import { ensureBookingHubStarted, getBookingHubConnection } from '@/lib/signalr';
import type { Booking, BookingStatus } from '@/lib/types';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Pagination } from '@/components/Pagination';
import { StatusBadge } from '@/components/StatusBadge';
import { CancelBookingModal } from '@/components/CancelBookingModal';

const PAGE_SIZE = 5;

const STATUS_FILTERS: { value: BookingStatus | ''; label: string }[] = [
  { value: '', label: 'Tất cả' },
  { value: 'Pending', label: 'Chờ xác nhận' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'Completed', label: 'Hoàn thành' },
  { value: 'Cancelled', label: 'Đã hủy' },
];

// Chỉ booking ở 2 trạng thái này mới còn hủy được (khớp business rule ở backend:
// không hủy được booking đã Completed, và BookingService còn tự chặn thêm nếu StartTime đã qua).
const CANCELLABLE_STATUSES: BookingStatus[] = ['Pending', 'Confirmed'];

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function BookingsList() {
  const searchParams = useSearchParams();
  const justCreated = searchParams.get('created') === '1';

  const [bookings, setBookings] = useState<Booking[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<BookingStatus | ''>('');

  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');
  const [cancelingBooking, setCancelingBooking] = useState<Booking | null>(null);

  const fetchBookings = useCallback(async () => {
    setStatus('loading');
    try {
      const res = await bookingsApi.getMyBookings({
        status: statusFilter || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      });
      const paged = res.data.data;
      setBookings(paged?.items ?? []);
      setTotalPages(paged?.totalPages ?? 0);
      setStatus('ready');
    } catch (err) {
      setErrorMessage(getErrorMessage(err, 'Không tải được danh sách lịch đặt.'));
      setStatus('error');
    }
  }, [statusFilter, pageNumber]);

  useEffect(() => {
    fetchBookings();
  }, [fetchBookings]);

  // Realtime: khi Admin xác nhận/hoàn thành/hủy 1 booking (kể cả job Hangfire tự động xử lý
  // booking quá hạn), backend bắn sự kiện "BookingChanged" qua SignalR - trang này tự tải lại
  // danh sách ngay, khách hàng không cần bấm F5 mới thấy trạng thái mới nhất.
  useEffect(() => {
    const connection = getBookingHubConnection();
    const handleBookingChanged = () => fetchBookings();

    connection.on('BookingChanged', handleBookingChanged);
    ensureBookingHubStarted();

    return () => {
      connection.off('BookingChanged', handleBookingChanged);
    };
  }, [fetchBookings]);

  useEffect(() => {
    setPageNumber(1);
  }, [statusFilter]);

  async function handleCancelConfirm(reason: string) {
    if (!cancelingBooking) return;
    try {
      await bookingsApi.cancel(cancelingBooking.id, reason);
      setCancelingBooking(null);
      fetchBookings();
    } catch (err) {
      // Ném lại dạng Error với message tiếng Việt lấy từ ApiResponse.message của backend
      // (vd: "Không thể hủy booking đã hoàn thành."), để CancelBookingModal hiển thị đúng nội dung.
      throw new Error(getErrorMessage(err, 'Hủy booking thất bại.'));
    }
  }

  return (
    <div>
      {justCreated && (
        <div className="mb-6 rounded-md border border-sage-300 bg-sage-50 px-4 py-3 text-sm text-sage-700">
          Đặt lịch thành công! Lịch của bạn đang ở trạng thái chờ xác nhận.
        </div>
      )}

      <div className="flex flex-wrap items-center gap-2">
        {STATUS_FILTERS.map((f) => (
          <button
            key={f.value || 'all'}
            onClick={() => setStatusFilter(f.value)}
            className={
              statusFilter === f.value
                ? 'rounded-full bg-sage-600 px-3.5 py-1.5 text-xs font-medium text-white'
                : 'rounded-full border border-stone-200 bg-white px-3.5 py-1.5 text-xs font-medium text-ink-light hover:bg-stone-100'
            }
          >
            {f.label}
          </button>
        ))}
      </div>

      <div className="mt-6">
        {status === 'loading' && <LoadingState label="Đang tải lịch đặt…" />}

        {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchBookings} />}

        {status === 'ready' && bookings.length === 0 && (
          <EmptyState
            title="Chưa có lịch đặt nào"
            description={
              statusFilter
                ? 'Không có lịch đặt nào ở trạng thái này.'
                : 'Hãy đặt lịch dịch vụ đầu tiên của bạn.'
            }
          />
        )}

        {status === 'ready' && bookings.length > 0 && (
          <div className="flex flex-col gap-3">
            {bookings.map((booking) => (
              <div key={booking.id} className="card rounded-lg p-5 shadow-sm">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium text-ink">{booking.bookingCode}</span>
                      <StatusBadge status={booking.status} />
                    </div>
                    <h3 className="mt-1 text-base font-semibold text-ink">{booking.serviceName}</h3>
                    <p className="mt-1 text-sm text-ink-light">
                      {formatDateTime(booking.startTime)} – {formatDateTime(booking.endTime)}
                    </p>
                    <p className="text-sm text-ink-light">Nhân viên: {booking.staffName}</p>
                    {booking.customerNote && (
                      <p className="mt-2 text-sm text-ink-light">Ghi chú: {booking.customerNote}</p>
                    )}
                    {booking.status === 'Cancelled' && booking.cancellationReason && (
                      <p className="mt-2 text-sm text-rose-500">
                        Lý do hủy: {booking.cancellationReason}
                      </p>
                    )}
                  </div>

                  {CANCELLABLE_STATUSES.includes(booking.status) && (
                    <button
                      className="btn-secondary shrink-0 text-xs"
                      onClick={() => setCancelingBooking(booking)}
                    >
                      Hủy lịch
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {status === 'ready' && (
          <Pagination pageNumber={pageNumber} totalPages={totalPages} onChange={setPageNumber} />
        )}
      </div>

      {cancelingBooking && (
        <CancelBookingModal
          booking={cancelingBooking}
          onClose={() => setCancelingBooking(null)}
          onConfirm={handleCancelConfirm}
        />
      )}
    </div>
  );
}
