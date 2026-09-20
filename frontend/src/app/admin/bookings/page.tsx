'use client';

import { useCallback, useEffect, useState } from 'react';
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

const PAGE_SIZE = 8;

const STATUS_FILTERS: { value: BookingStatus | ''; label: string }[] = [
  { value: '', label: 'Tất cả' },
  { value: 'Pending', label: 'Chờ xác nhận' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'Completed', label: 'Hoàn thành' },
  { value: 'Cancelled', label: 'Đã hủy' },
];

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function AdminBookingsPage() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<BookingStatus | ''>('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');
  const [cancelingBooking, setCancelingBooking] = useState<Booking | null>(null);
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  const fetchBookings = useCallback(async () => {
    setStatus('loading');
    try {
      const res = await bookingsApi.getAll({
        status: statusFilter || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      });
      const paged = res.data.data;
      setBookings(paged?.items ?? []);
      setTotalPages(paged?.totalPages ?? 0);
      setStatus('ready');
    } catch (err) {
      setErrorMessage(getErrorMessage(err, 'Không tải được danh sách booking.'));
      setStatus('error');
    }
  }, [statusFilter, fromDate, toDate, pageNumber]);

  useEffect(() => {
    fetchBookings();
  }, [fetchBookings]);

  // Realtime: thấy ngay booking mới Customer vừa tạo, hoặc khi Hangfire tự động hủy/hoàn thành
  // booking quá hạn ở background - không cần F5 lại trang quản trị liên tục để kiểm tra.
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
  }, [statusFilter, fromDate, toDate]);

  async function handleUpdateStatus(booking: Booking, nextStatus: BookingStatus) {
    setUpdatingId(booking.id);
    try {
      await bookingsApi.updateStatus(booking.id, { status: nextStatus });
      fetchBookings();
    } catch (err) {
      alert(getErrorMessage(err, 'Cập nhật trạng thái thất bại.'));
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleCancelConfirm(reason: string) {
    if (!cancelingBooking) return;
    try {
      await bookingsApi.cancel(cancelingBooking.id, reason);
      setCancelingBooking(null);
      fetchBookings();
    } catch (err) {
      throw new Error(getErrorMessage(err, 'Hủy booking thất bại.'));
    }
  }

  return (
    <div>
      <div>
        <h1 className="font-display text-2xl font-semibold text-ink">Quản lý booking</h1>
        <p className="text-sm text-ink-light">Xem, lọc, xác nhận/hoàn thành/hủy booking.</p>
      </div>

      <div className="mt-6 flex flex-wrap items-end gap-4">
        <div className="flex flex-wrap gap-2">
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

        <div className="flex items-end gap-2">
          <div>
            <label htmlFor="from" className="block text-xs text-ink-light">
              Từ ngày
            </label>
            <input
              id="from"
              type="date"
              className="input-field mt-1 text-sm"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
          </div>
          <div>
            <label htmlFor="to" className="block text-xs text-ink-light">
              Đến ngày
            </label>
            <input
              id="to"
              type="date"
              className="input-field mt-1 text-sm"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
          </div>
          {(fromDate || toDate) && (
            <button
              className="btn-secondary text-xs"
              onClick={() => {
                setFromDate('');
                setToDate('');
              }}
            >
              Xóa lọc ngày
            </button>
          )}
        </div>
      </div>

      <div className="mt-6">
        {status === 'loading' && <LoadingState label="Đang tải danh sách booking…" />}

        {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchBookings} />}

        {status === 'ready' && bookings.length === 0 && (
          <EmptyState title="Không có booking nào khớp bộ lọc" />
        )}

        {status === 'ready' && bookings.length > 0 && (
          <div className="flex flex-col gap-3">
            {bookings.map((booking) => {
              const isUpdating = updatingId === booking.id;

              return (
                <div key={booking.id} className="card rounded-lg p-5 shadow-sm">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-ink">{booking.bookingCode}</span>
                        <StatusBadge status={booking.status} />
                      </div>
                      <h3 className="mt-1 text-base font-semibold text-ink">{booking.serviceName}</h3>
                      <p className="mt-1 text-sm text-ink-light">
                        Khách hàng: <span className="text-ink">{booking.customerName}</span> · Nhân
                        viên: <span className="text-ink">{booking.staffName}</span>
                      </p>
                      <p className="text-sm text-ink-light">
                        {formatDateTime(booking.startTime)} – {formatDateTime(booking.endTime)}
                      </p>
                      {booking.customerNote && (
                        <p className="mt-2 text-sm text-ink-light">Ghi chú: {booking.customerNote}</p>
                      )}
                      {booking.status === 'Cancelled' && booking.cancellationReason && (
                        <p className="mt-2 text-sm text-rose-500">
                          Lý do hủy: {booking.cancellationReason}
                        </p>
                      )}
                    </div>

                    <div className="flex shrink-0 flex-wrap gap-2">
                      {booking.status === 'Pending' && (
                        <button
                          className="btn-primary px-3 py-1.5 text-xs"
                          disabled={isUpdating}
                          onClick={() => handleUpdateStatus(booking, 'Confirmed')}
                        >
                          {isUpdating ? '…' : 'Xác nhận'}
                        </button>
                      )}
                      {booking.status === 'Confirmed' && (
                        <button
                          className="btn-primary px-3 py-1.5 text-xs"
                          disabled={isUpdating}
                          onClick={() => handleUpdateStatus(booking, 'Completed')}
                        >
                          {isUpdating ? '…' : 'Hoàn thành'}
                        </button>
                      )}
                      {(booking.status === 'Pending' || booking.status === 'Confirmed') && (
                        <button
                          className="btn-secondary px-3 py-1.5 text-xs"
                          disabled={isUpdating}
                          onClick={() => setCancelingBooking(booking)}
                        >
                          Hủy
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
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
