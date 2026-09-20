'use client';

import { FormEvent, useState } from 'react';
import { Modal } from '@/components/Modal';
import { staffsApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Staff } from '@/lib/types';

interface StaffFormModalProps {
  staff: Staff | null; // null = tạo mới
  onClose: () => void;
  onSaved: () => void;
}

export function StaffFormModal({ staff, onClose, onSaved }: StaffFormModalProps) {
  const isEditing = staff !== null;

  const [fullName, setFullName] = useState(staff?.fullName ?? '');
  const [email, setEmail] = useState(staff?.email ?? '');
  const [isActive, setIsActive] = useState(staff?.isActive ?? true);

  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    if (!fullName.trim() || !email.trim()) {
      setError('Họ tên và email là bắt buộc.');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      if (isEditing) {
        await staffsApi.update(staff.id, { fullName: fullName.trim(), email: email.trim(), isActive });
      } else {
        await staffsApi.create({ fullName: fullName.trim(), email: email.trim() });
      }
      onSaved();
    } catch (err) {
      setError(getErrorMessage(err, 'Lưu nhân viên thất bại.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Modal title={isEditing ? 'Sửa nhân viên' : 'Thêm nhân viên mới'} onClose={onClose}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label htmlFor="staff-name" className="block text-sm font-medium text-ink">
            Họ tên <span className="text-rose-500">*</span>
          </label>
          <input
            id="staff-name"
            className="input-field mt-1.5"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div>
          <label htmlFor="staff-email" className="block text-sm font-medium text-ink">
            Email <span className="text-rose-500">*</span>
          </label>
          <input
            id="staff-email"
            type="email"
            className="input-field mt-1.5"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        {isEditing && (
          <label className="flex items-center gap-2 text-sm text-ink">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              disabled={isSubmitting}
              className="h-4 w-4 rounded border-stone-200 text-sage-600 focus:ring-sage-500"
            />
            Đang hoạt động (bỏ chọn để khóa nhân viên)
          </label>
        )}

        {error && (
          <p className="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-500" role="alert">
            {error}
          </p>
        )}

        <div className="mt-2 flex justify-end gap-3">
          <button type="button" className="btn-secondary" onClick={onClose} disabled={isSubmitting}>
            Hủy
          </button>
          <button type="submit" className="btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Đang lưu…' : 'Lưu'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
