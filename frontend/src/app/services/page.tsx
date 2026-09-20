'use client';

import { useCallback, useEffect, useState } from 'react';
import { servicesApi } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-client';
import type { Service } from '@/lib/types';
import { useDebounce } from '@/hooks/useDebounce';
import { ServiceCard } from '@/components/ServiceCard';
import { LoadingState } from '@/components/LoadingState';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { Pagination } from '@/components/Pagination';

const PAGE_SIZE = 6;

export default function ServicesPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400);

  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading');
  const [errorMessage, setErrorMessage] = useState('');

  const fetchServices = useCallback(async () => {
    setStatus('loading');
    try {
      const res = await servicesApi.getList({
        search: debouncedSearch || undefined,
        isActive: true, // Customer chỉ nên thấy dịch vụ đang mở đặt lịch
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
  }, [debouncedSearch, pageNumber]);

  useEffect(() => {
    fetchServices();
  }, [fetchServices]);

  // Đổi từ khóa tìm kiếm -> luôn quay về trang 1
  useEffect(() => {
    setPageNumber(1);
  }, [debouncedSearch]);

  return (
    <div>
      <div className="flex flex-col gap-1">
        <h1 className="font-display text-2xl font-semibold text-ink">Dịch vụ</h1>
        <p className="text-sm text-ink-light">Chọn dịch vụ bạn muốn đặt lịch.</p>
      </div>

      <div className="mt-6 max-w-sm">
        <input
          type="search"
          className="input-field"
          placeholder="Tìm theo tên dịch vụ…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="mt-6">
        {status === 'loading' && <LoadingState label="Đang tải danh sách dịch vụ…" />}

        {status === 'error' && <ErrorState message={errorMessage} onRetry={fetchServices} />}

        {status === 'ready' && services.length === 0 && (
          <EmptyState
            title="Không tìm thấy dịch vụ phù hợp"
            description={
              debouncedSearch
                ? `Không có dịch vụ nào khớp với "${debouncedSearch}". Thử từ khóa khác.`
                : 'Hiện chưa có dịch vụ nào đang mở đặt lịch.'
            }
          />
        )}

        {status === 'ready' && services.length > 0 && (
          <div className="flex flex-col gap-3">
            {services.map((service) => (
              <ServiceCard key={service.id} service={service} />
            ))}
          </div>
        )}

        {status === 'ready' && (
          <Pagination pageNumber={pageNumber} totalPages={totalPages} onChange={setPageNumber} />
        )}
      </div>
    </div>
  );
}
