'use client';

import { useState } from 'react';
import type { Booking } from '@/lib/types';

interface CancelBookingModalProps {
  booking: Booking;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

export function CancelBookingModal({ booking, onClose, onConfirm }: CancelBookingModalProps) {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const trimmedReason = reason.trim();

  async function handleConfirm() {
    if (!trimmedReason) {
      setError('Vui lòng nhập lý do hủy.');
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      await onConfirm(trimmedReason);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Hủy booking thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink/40 px-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h2 className="font-display text-lg font-semibold text-ink">Hủy lịch đặt</h2>
        <p className="mt-1 text-sm text-ink-light">
          Mã đặt lịch <span className="font-medium text-ink">{booking.bookingCode}</span> —{' '}
          {booking.serviceName} với {booking.staffName}
        </p>

        <label className="mt-4 block text-sm font-medium text-ink" htmlFor="cancel-reason">
          Lý do hủy <span className="text-rose-500">*</span>
        </label>
        <textarea
          id="cancel-reason"
          className="input-field mt-1.5 min-h-24 resize-none"
          placeholder="Ví dụ: đổi lịch cá nhân, bận đột xuất…"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          disabled={isSubmitting}
          autoFocus
        />

        {error && <p className="mt-2 text-sm text-rose-500">{error}</p>}

        <div className="mt-6 flex justify-end gap-3">
          <button className="btn-secondary" onClick={onClose} disabled={isSubmitting}>
            Đóng
          </button>
          <button className="btn-danger" onClick={handleConfirm} disabled={isSubmitting}>
            {isSubmitting ? 'Đang hủy…' : 'Xác nhận hủy'}
          </button>
        </div>
      </div>
    </div>
  );
}
