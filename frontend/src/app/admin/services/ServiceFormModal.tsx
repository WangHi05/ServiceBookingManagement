'use client';

import { FormEvent, useState } from 'react';
import { Modal } from '@/components/Modal';
import { servicesApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Service } from '@/lib/types';

interface ServiceFormModalProps {
  service: Service | null; // null = tạo mới, có giá trị = đang sửa
  onClose: () => void;
  onSaved: () => void;
}

export function ServiceFormModal({ service, onClose, onSaved }: ServiceFormModalProps) {
  const isEditing = service !== null;

  const [name, setName] = useState(service?.name ?? '');
  const [description, setDescription] = useState(service?.description ?? '');
  const [durationMinutes, setDurationMinutes] = useState(service?.durationMinutes ?? 30);
  const [price, setPrice] = useState(service?.price ?? 0);
  const [isActive, setIsActive] = useState(service?.isActive ?? true);

  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    // Validate lại phía client theo đúng quy tắc backend (mục 2 đề bài),
    // dù backend luôn kiểm tra lại nên đây chỉ là UX tốt hơn, không thay thế validate server.
    if (!name.trim()) {
      setError('Tên dịch vụ là bắt buộc.');
      return;
    }
    if (durationMinutes <= 0) {
      setError('Thời lượng phải lớn hơn 0.');
      return;
    }
    if (price < 0) {
      setError('Giá không được âm.');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      if (isEditing) {
        await servicesApi.update(service.id, {
          name: name.trim(),
          description: description.trim() || undefined,
          durationMinutes,
          price,
          isActive,
        });
      } else {
        await servicesApi.create({
          name: name.trim(),
          description: description.trim() || undefined,
          durationMinutes,
          price,
        });
      }
      onSaved();
    } catch (err) {
      setError(getErrorMessage(err, 'Lưu dịch vụ thất bại.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Modal title={isEditing ? 'Sửa dịch vụ' : 'Thêm dịch vụ mới'} onClose={onClose}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label htmlFor="svc-name" className="block text-sm font-medium text-ink">
            Tên dịch vụ <span className="text-rose-500">*</span>
          </label>
          <input
            id="svc-name"
            className="input-field mt-1.5"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div>
          <label htmlFor="svc-desc" className="block text-sm font-medium text-ink">
            Mô tả
          </label>
          <textarea
            id="svc-desc"
            className="input-field mt-1.5 min-h-20 resize-none"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="svc-duration" className="block text-sm font-medium text-ink">
              Thời lượng (phút) <span className="text-rose-500">*</span>
            </label>
            <input
              id="svc-duration"
              type="number"
              min={1}
              className="input-field mt-1.5"
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(Number(e.target.value))}
              disabled={isSubmitting}
            />
          </div>

          <div>
            <label htmlFor="svc-price" className="block text-sm font-medium text-ink">
              Giá (VNĐ) <span className="text-rose-500">*</span>
            </label>
            <input
              id="svc-price"
              type="number"
              min={0}
              step={1000}
              className="input-field mt-1.5"
              value={price}
              onChange={(e) => setPrice(Number(e.target.value))}
              disabled={isSubmitting}
            />
          </div>
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
            Đang mở cho đặt lịch (bỏ chọn để khóa dịch vụ)
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
