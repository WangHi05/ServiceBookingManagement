'use client';

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import clsx from 'clsx';
import { bookingsApi, servicesApi, staffsApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { AvailableSlot, Service, Staff } from '@/lib/types';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatSlotTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

export function BookingForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const preselectedServiceId = searchParams.get('serviceId');

  const [services, setServices] = useState<Service[]>([]);
  const [staffs, setStaffs] = useState<Staff[]>([]);
  const [isLoadingOptions, setIsLoadingOptions] = useState(true);
  const [optionsError, setOptionsError] = useState<string | null>(null);

  const [serviceId, setServiceId] = useState<number | ''>('');
  const [staffId, setStaffId] = useState<number | ''>(''); // '' = tất cả nhân viên
  const [date, setDate] = useState(todayIso());
  const [note, setNote] = useState('');

  const [slots, setSlots] = useState<AvailableSlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlot | null>(null);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [slotsError, setSlotsError] = useState<string | null>(null);

  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Tải danh sách dịch vụ + nhân viên 1 lần khi vào trang
  useEffect(() => {
    async function loadOptions() {
      setIsLoadingOptions(true);
      try {
        const [servicesRes, staffsRes] = await Promise.all([
          servicesApi.getList({ isActive: true, pageNumber: 1, pageSize: 100 }),
          staffsApi.getList(true),
        ]);

        const serviceList = servicesRes.data.data?.items ?? [];
        setServices(serviceList);
        setStaffs(staffsRes.data.data ?? []);

        if (preselectedServiceId) {
          const preselected = serviceList.find((s) => s.id === Number(preselectedServiceId));
          if (preselected) setServiceId(preselected.id);
        }

        setOptionsError(null);
      } catch (err) {
        setOptionsError(getErrorMessage(err, 'Không tải được dịch vụ/nhân viên.'));
      } finally {
        setIsLoadingOptions(false);
      }
    }

    loadOptions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedService = useMemo(
    () => services.find((s) => s.id === serviceId) ?? null,
    [services, serviceId]
  );

  const fetchSlots = useCallback(async () => {
    if (!serviceId || !date) return;

    setIsLoadingSlots(true);
    setSlotsError(null);
    setSelectedSlot(null);

    try {
      const res = await bookingsApi.getAvailableSlots(
        Number(serviceId),
        date,
        staffId ? Number(staffId) : undefined
      );
      setSlots(res.data.data ?? []);
    } catch (err) {
      setSlotsError(getErrorMessage(err, 'Không tải được khung giờ trống.'));
      setSlots([]);
    } finally {
      setIsLoadingSlots(false);
    }
  }, [serviceId, date, staffId]);

  useEffect(() => {
    fetchSlots();
  }, [fetchSlots]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!serviceId || !selectedSlot) return;

    setSubmitError(null);
    setIsSubmitting(true);

    try {
      await bookingsApi.create({
        serviceId: Number(serviceId),
        staffId: selectedSlot.staffId,
        startTime: selectedSlot.startTime,
        customerNote: note.trim() || undefined,
      });

      router.push('/my-bookings?created=1');
    } catch (err) {
      // 409 (trùng lịch) là trường hợp phổ biến nhất ở đây: khung giờ vừa bị người khác đặt
      // trước khi mình bấm Xác nhận -> báo rõ và tự tải lại danh sách khung giờ.
      setSubmitError(getErrorMessage(err, 'Đặt lịch thất bại. Vui lòng thử lại.'));
      fetchSlots();
    } finally {
      setIsSubmitting(false);
    }
  }

  if (isLoadingOptions) return <LoadingState label="Đang tải dữ liệu đặt lịch…" />;

  if (optionsError) {
    return (
      <EmptyState
        title="Không thể tải trang đặt lịch"
        description={optionsError}
        action={
          <button className="btn-secondary" onClick={() => window.location.reload()}>
            Thử lại
          </button>
        }
      />
    );
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-8 md:grid-cols-2">
      {/* Cột trái: chọn dịch vụ / nhân viên / ngày */}
      <div className="flex flex-col gap-5">
        <div>
          <label htmlFor="service" className="block text-sm font-medium text-ink">
            Dịch vụ <span className="text-rose-500">*</span>
          </label>
          <select
            id="service"
            className="input-field mt-1.5"
            value={serviceId}
            onChange={(e) => setServiceId(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">— Chọn dịch vụ —</option>
            {services.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name} ({s.durationMinutes} phút)
              </option>
            ))}
          </select>
          {selectedService?.description && (
            <p className="mt-1.5 text-xs text-ink-light">{selectedService.description}</p>
          )}
        </div>

        <div>
          <label htmlFor="staff" className="block text-sm font-medium text-ink">
            Nhân viên
          </label>
          <select
            id="staff"
            className="input-field mt-1.5"
            value={staffId}
            onChange={(e) => setStaffId(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">Tất cả nhân viên</option>
            {staffs.map((s) => (
              <option key={s.id} value={s.id}>
                {s.fullName}
              </option>
            ))}
          </select>
          <p className="mt-1.5 text-xs text-ink-light">
            Để trống để xem khung giờ trống của mọi nhân viên.
          </p>
        </div>

        <div>
          <label htmlFor="date" className="block text-sm font-medium text-ink">
            Ngày <span className="text-rose-500">*</span>
          </label>
          <input
            id="date"
            type="date"
            className="input-field mt-1.5"
            min={todayIso()}
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </div>

        <div>
          <label htmlFor="note" className="block text-sm font-medium text-ink">
            Ghi chú
          </label>
          <textarea
            id="note"
            className="input-field mt-1.5 min-h-24 resize-none"
            placeholder="Yêu cầu thêm cho nhân viên (không bắt buộc)…"
            value={note}
            onChange={(e) => setNote(e.target.value)}
          />
        </div>
      </div>

      {/* Cột phải: chọn khung giờ trống */}
      <div>
        <p className="block text-sm font-medium text-ink">
          Khung giờ trống <span className="text-rose-500">*</span>
        </p>

        <div className="mt-1.5 min-h-[16rem] rounded-md border border-stone-200 bg-white p-4">
          {!serviceId && (
            <p className="py-10 text-center text-sm text-ink-light">
              Chọn dịch vụ và ngày để xem khung giờ trống.
            </p>
          )}

          {serviceId && isLoadingSlots && <LoadingState label="Đang tìm khung giờ trống…" />}

          {serviceId && !isLoadingSlots && slotsError && (
            <p className="py-10 text-center text-sm text-rose-500">{slotsError}</p>
          )}

          {serviceId && !isLoadingSlots && !slotsError && slots.length === 0 && (
            <p className="py-10 text-center text-sm text-ink-light">
              Không còn khung giờ trống cho ngày này. Thử chọn ngày khác hoặc nhân viên khác.
            </p>
          )}

          {serviceId && !isLoadingSlots && !slotsError && slots.length > 0 && (
            <div className="grid grid-cols-3 gap-2">
              {slots.map((slot) => {
                const isSelected =
                  selectedSlot?.staffId === slot.staffId &&
                  selectedSlot?.startTime === slot.startTime;

                return (
                  <button
                    type="button"
                    key={`${slot.staffId}-${slot.startTime}`}
                    onClick={() => setSelectedSlot(slot)}
                    className={clsx(
                      'rounded-md border px-2 py-2 text-xs transition-colors',
                      isSelected
                        ? 'border-sage-600 bg-sage-600 text-white'
                        : 'border-stone-200 bg-white text-ink hover:border-sage-300'
                    )}
                  >
                    <div className="font-medium">{formatSlotTime(slot.startTime)}</div>
                    {!staffId && <div className="mt-0.5 opacity-80">{slot.staffName}</div>}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {selectedSlot && (
          <p className="mt-3 text-sm text-sage-700">
            Đã chọn: {formatSlotTime(selectedSlot.startTime)}–{formatSlotTime(selectedSlot.endTime)}{' '}
            với {selectedSlot.staffName}
          </p>
        )}

        {submitError && (
          <p className="mt-3 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-500" role="alert">
            {submitError}
          </p>
        )}

        <button
          type="submit"
          className="btn-primary mt-4 w-full"
          disabled={!serviceId || !selectedSlot || isSubmitting}
        >
          {isSubmitting ? 'Đang đặt lịch…' : 'Xác nhận đặt lịch'}
        </button>
      </div>
    </form>
  );
}
