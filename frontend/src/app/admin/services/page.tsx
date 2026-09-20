'use client';

import { useCallback, useEffect, useState } from 'react';
import { servicesApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Service } from '@/lib/types';
import { useDebounce } from '@/hooks/useDebounce';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Pagination } from '@/components/Pagination';
import { ServiceFormModal } from './ServiceFormModal';

const PAGE_SIZE = 8;

function formatPrice(price: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
}

type ActiveFilter = 'all' | 'active' | 'inactive';

export default function AdminServicesPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400);
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>('all');

  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');
  const [modalState, setModalState] = useState<'closed' | 'create' | Service>('closed');
  const [togglingId, setTogglingId] = useState<number | null>(null);

  const fetchServices = useCallback(async () => {
    setStatus('loading');
    try {
      const res = await servicesApi.getList({
        search: debouncedSearch || undefined,
        isActive: activeFilter === 'all' ? undefined : activeFilter === 'active',
        pageNumber,
        pageSize: PAGE_SIZE,
      });
      const paged = res.data.data;
      setServices(paged?.items ?? []);
      setTotalPages(paged?.totalPages ?? 0);
      setStatus('ready');
    } catch (err) {
      setErrorMessage(getErrorMessage(err, 'Không tải được danh sách dịch vụ.'));
      setStatus('error');
    }
  }, [debouncedSearch, activeFilter, pageNumber]);

  useEffect(() => {
    fetchServices();
  }, [fetchServices]);

  useEffect(() => {
    setPageNumber(1);
  }, [debouncedSearch, activeFilter]);

  async function handleToggleActive(service: Service) {
    setTogglingId(service.id);
    try {
      await servicesApi.update(service.id, {
        name: service.name,
        description: service.description ?? undefined,
        durationMinutes: service.durationMinutes,
        price: service.price,
        isActive: !service.isActive,
      });
      fetchServices();
    } catch (err) {
      alert(getErrorMessage(err, 'Không thể đổi trạng thái dịch vụ.'));
    } finally {
      setTogglingId(null);
    }
  }

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink">Quản lý dịch vụ</h1>
          <p className="text-sm text-ink-light">Thêm, sửa, khóa/mở khóa dịch vụ.</p>
        </div>
        <button className="btn-primary" onClick={() => setModalState('create')}>
          + Thêm dịch vụ
        </button>
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <input
          type="search"
          className="input-field max-w-xs"
          placeholder="Tìm theo tên dịch vụ…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />

        <div className="flex gap-2">
          {(
            [
              { value: 'all', label: 'Tất cả' },
              { value: 'active', label: 'Đang mở' },
              { value: 'inactive', label: 'Đã khóa' },
            ] as { value: ActiveFilter; label: string }[]
          ).map((f) => (
            <button
              key={f.value}
              onClick={() => setActiveFilter(f.value)}
              className={
                activeFilter === f.value
                  ? 'rounded-full bg-sage-600 px-3.5 py-1.5 text-xs font-medium text-white'
                  : 'rounded-full border border-stone-200 bg-white px-3.5 py-1.5 text-xs font-medium text-ink-light hover:bg-stone-100'
              }
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-6">
        {status === 'loading' && <LoadingState label="Đang tải danh sách dịch vụ…" />}

        {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchServices} />}

        {status === 'ready' && services.length === 0 && (
          <EmptyState
            title="Chưa có dịch vụ nào"
            description="Bấm 'Thêm dịch vụ' để tạo dịch vụ đầu tiên."
          />
        )}

        {status === 'ready' && services.length > 0 && (
          <div className="overflow-x-auto rounded-lg border border-stone-200 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-stone-200 bg-stone-50 text-xs uppercase text-ink-light">
                <tr>
                  <th className="px-4 py-3 font-medium">Tên dịch vụ</th>
                  <th className="px-4 py-3 font-medium">Thời lượng</th>
                  <th className="px-4 py-3 font-medium">Giá</th>
                  <th className="px-4 py-3 font-medium">Trạng thái</th>
                  <th className="px-4 py-3 font-medium">Hành động</th>
                </tr>
              </thead>
              <tbody>
                {services.map((service) => (
                  <tr key={service.id} className="border-b border-stone-100 last:border-0">
                    <td className="px-4 py-3">
                      <div className="font-medium text-ink">{service.name}</div>
                      {service.description && (
                        <div className="mt-0.5 line-clamp-1 text-xs text-ink-light">
                          {service.description}
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3 text-ink-light">{service.durationMinutes} phút</td>
                    <td className="px-4 py-3 text-ink-light">{formatPrice(service.price)}</td>
                    <td className="px-4 py-3">
                      <span
                        className={
                          service.isActive
                            ? 'rounded-full bg-sage-100 px-2.5 py-1 text-xs font-medium text-sage-700'
                            : 'rounded-full bg-stone-100 px-2.5 py-1 text-xs font-medium text-ink-light'
                        }
                      >
                        {service.isActive ? 'Đang mở' : 'Đã khóa'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <button
                          className="btn-secondary px-3 py-1.5 text-xs"
                          onClick={() => setModalState(service)}
                        >
                          Sửa
                        </button>
                        <button
                          className="btn-secondary px-3 py-1.5 text-xs"
                          onClick={() => handleToggleActive(service)}
                          disabled={togglingId === service.id}
                        >
                          {togglingId === service.id
                            ? '…'
                            : service.isActive
                              ? 'Khóa'
                              : 'Mở khóa'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {status === 'ready' && (
          <Pagination pageNumber={pageNumber} totalPages={totalPages} onChange={setPageNumber} />
        )}
      </div>

      {modalState !== 'closed' && (
        <ServiceFormModal
          service={modalState === 'create' ? null : modalState}
          onClose={() => setModalState('closed')}
          onSaved={() => {
            setModalState('closed');
            fetchServices();
          }}
        />
      )}
    </div>
  );
}
