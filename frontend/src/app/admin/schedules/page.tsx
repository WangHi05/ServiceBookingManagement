'use client';

import { useCallback, useEffect, useState } from 'react';
import { staffsApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Staff } from '@/lib/types';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { StaffFormModal } from './StaffFormModal';
import { StaffSchedulePanel } from './StaffSchedulePanel';

export default function AdminSchedulesPage() {
  const [staffs, setStaffs] = useState<Staff[]>([]);
  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');
  const [selectedStaffId, setSelectedStaffId] = useState<number | null>(null);
  const [modalState, setModalState] = useState<'closed' | 'create' | Staff>('closed');

  const fetchStaffs = useCallback(async () => {
    setStatus('loading');
    try {
      const res = await staffsApi.getList();
      const list = res.data.data ?? [];
      setStaffs(list);
      setStatus('ready');

      // Tự chọn nhân viên đầu tiên nếu chưa chọn gì (hoặc nhân viên đang chọn đã bị xóa khỏi danh sách)
      setSelectedStaffId((current) => {
        if (current && list.some((s) => s.id === current)) return current;
        return list[0]?.id ?? null;
      });
    } catch (err) {
      setErrorMessage(getErrorMessage(err, 'Không tải được danh sách nhân viên.'));
      setStatus('error');
    }
  }, []);

  useEffect(() => {
    fetchStaffs();
  }, [fetchStaffs]);

  const selectedStaff = staffs.find((s) => s.id === selectedStaffId) ?? null;

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink">Nhân viên & Lịch làm việc</h1>
          <p className="text-sm text-ink-light">Chọn 1 nhân viên để xem/thêm ca làm việc.</p>
        </div>
        <button className="btn-primary" onClick={() => setModalState('create')}>
          + Thêm nhân viên
        </button>
      </div>

      <div className="mt-6 grid gap-6 md:grid-cols-[16rem_1fr]">
        {/* Danh sách nhân viên */}
        <div>
          {status === 'loading' && <LoadingState label="Đang tải…" />}
          {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchStaffs} />}
          {status === 'ready' && staffs.length === 0 && (
            <EmptyState title="Chưa có nhân viên nào" description="Bấm 'Thêm nhân viên' để bắt đầu." />
          )}
          {status === 'ready' && staffs.length > 0 && (
            <div className="flex flex-col gap-1 rounded-lg border border-stone-200 bg-white p-2">
              {staffs.map((staff) => (
                <button
                  key={staff.id}
                  onClick={() => setSelectedStaffId(staff.id)}
                  className={
                    staff.id === selectedStaffId
                      ? 'flex items-center justify-between rounded-md bg-sage-50 px-3 py-2.5 text-left text-sm font-medium text-sage-700'
                      : 'flex items-center justify-between rounded-md px-3 py-2.5 text-left text-sm text-ink hover:bg-stone-50'
                  }
                >
                  <span className="truncate">{staff.fullName}</span>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      setModalState(staff);
                    }}
                    className="shrink-0 text-xs text-ink-light underline-offset-2 hover:underline"
                  >
                    Sửa
                  </button>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Panel lịch làm việc của nhân viên đang chọn */}
        <div>
          {selectedStaff ? (
            <StaffSchedulePanel staff={selectedStaff} />
          ) : (
            status === 'ready' && (
              <EmptyState title="Chọn 1 nhân viên ở danh sách bên trái để quản lý lịch làm việc." />
            )
          )}
        </div>
      </div>

      {modalState !== 'closed' && (
        <StaffFormModal
          staff={modalState === 'create' ? null : modalState}
          onClose={() => setModalState('closed')}
          onSaved={() => {
            setModalState('closed');
            fetchStaffs();
          }}
        />
      )}
    </div>
  );
}
