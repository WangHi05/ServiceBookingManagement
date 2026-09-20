'use client';

import { FormEvent, useCallback, useEffect, useState } from 'react';
import { staffsApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Staff, WorkSchedule } from '@/lib/types';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN', {
    weekday: 'short',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

function formatTime(hhmmss: string): string {
  return hhmmss.slice(0, 5); // "08:00:00" -> "08:00"
}

export function StaffSchedulePanel({ staff }: { staff: Staff }) {
  const [schedules, setSchedules] = useState<WorkSchedule[]>([]);
  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');

  const [workDate, setWorkDate] = useState(todayIso());
  const [startTime, setStartTime] = useState('08:00');
  const [endTime, setEndTime] = useState('17:00');
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchSchedules = useCallback(async () => {
    setStatus('loading');
    try {
      // Chỉ hiển thị lịch từ hôm nay trở đi để danh sách gọn, không cần xem lịch đã qua
      const res = await staffsApi.getSchedules(staff.id, todayIso());
      setSchedules(res.data.data ?? []);
      setStatus('ready');
    } catch (err) {
      setErrorMessage(getErrorMessage(err, 'Không tải được lịch làm việc.'));
      setStatus('error');
    }
  }, [staff.id]);

  useEffect(() => {
    fetchSchedules();
  }, [fetchSchedules]);

  async function handleAddSchedule(e: FormEvent) {
    e.preventDefault();

    if (startTime >= endTime) {
      setFormError('Giờ bắt đầu phải nhỏ hơn giờ kết thúc.');
      return;
    }

    setFormError(null);
    setIsSubmitting(true);

    try {
      await staffsApi.createSchedule(staff.id, {
        workDate,
        startTime: `${startTime}:00`,
        endTime: `${endTime}:00`,
      });
      fetchSchedules();
    } catch (err) {
      // Trường hợp phổ biến nhất ở đây là 409 (trùng ca) — hiển thị đúng message backend trả về
      setFormError(getErrorMessage(err, 'Tạo ca làm việc thất bại.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-base font-semibold text-ink">{staff.fullName}</h2>
          <p className="text-sm text-ink-light">{staff.email}</p>
        </div>
        <span
          className={
            staff.isActive
              ? 'rounded-full bg-sage-100 px-2.5 py-1 text-xs font-medium text-sage-700'
              : 'rounded-full bg-stone-100 px-2.5 py-1 text-xs font-medium text-ink-light'
          }
        >
          {staff.isActive ? 'Đang hoạt động' : 'Đã khóa'}
        </span>
      </div>

      {/* Form thêm ca làm việc mới */}
      <form onSubmit={handleAddSchedule} className="mt-5 rounded-lg border border-stone-200 bg-white p-4">
        <p className="text-sm font-medium text-ink">Thêm ca làm việc</p>
        <div className="mt-3 grid grid-cols-3 gap-3">
          <div>
            <label htmlFor="wd" className="block text-xs font-medium text-ink-light">
              Ngày
            </label>
            <input
              id="wd"
              type="date"
              className="input-field mt-1 text-sm"
              min={todayIso()}
              value={workDate}
              onChange={(e) => setWorkDate(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div>
            <label htmlFor="st" className="block text-xs font-medium text-ink-light">
              Giờ bắt đầu
            </label>
            <input
              id="st"
              type="time"
              className="input-field mt-1 text-sm"
              value={startTime}
              onChange={(e) => setStartTime(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div>
            <label htmlFor="et" className="block text-xs font-medium text-ink-light">
              Giờ kết thúc
            </label>
            <input
              id="et"
              type="time"
              className="input-field mt-1 text-sm"
              value={endTime}
              onChange={(e) => setEndTime(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
        </div>

        {formError && <p className="mt-3 text-sm text-rose-500">{formError}</p>}

        <button type="submit" className="btn-primary mt-3 text-sm" disabled={isSubmitting}>
          {isSubmitting ? 'Đang thêm…' : 'Thêm ca làm việc'}
        </button>
      </form>

      {/* Danh sách ca làm việc sắp tới */}
      <div className="mt-6">
        <p className="text-sm font-medium text-ink">Lịch làm việc sắp tới</p>

        <div className="mt-3">
          {status === 'loading' && <LoadingState label="Đang tải lịch làm việc…" />}
          {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchSchedules} />}
          {status === 'ready' && schedules.length === 0 && (
            <EmptyState title="Chưa có ca làm việc nào sắp tới" />
          )}
          {status === 'ready' && schedules.length > 0 && (
            <div className="flex flex-col divide-y divide-stone-100 rounded-lg border border-stone-200 bg-white">
              {schedules.map((s) => (
                <div key={s.id} className="flex items-center justify-between px-4 py-3 text-sm">
                  <span className="text-ink">{formatDate(s.workDate)}</span>
                  <span className="text-ink-light">
                    {formatTime(s.startTime)} – {formatTime(s.endTime)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
