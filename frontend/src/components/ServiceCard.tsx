import Link from 'next/link';
import type { Service } from '@/lib/types';

function formatPrice(price: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
}

export function ServiceCard({ service }: { service: Service }) {
  return (
    <div className="flex items-start justify-between gap-6 border-l-2 border-sage-500 bg-white py-5 pl-5 pr-6 shadow-sm">
      <div className="min-w-0">
        <h3 className="text-base font-semibold text-ink">{service.name}</h3>
        {service.description && (
          <p className="mt-1 text-sm text-ink-light">{service.description}</p>
        )}
        <div className="mt-2 flex items-center gap-3 text-sm text-ink-light">
          <span>{service.durationMinutes} phút</span>
          <span aria-hidden>•</span>
          <span className="font-medium text-sage-700">{formatPrice(service.price)}</span>
        </div>
      </div>

      <Link
        href={`/booking?serviceId=${service.id}`}
        className="btn-primary shrink-0 whitespace-nowrap"
      >
        Đặt lịch
      </Link>
    </div>
  );
}
