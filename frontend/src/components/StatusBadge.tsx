import type { BookingStatus } from '@/lib/types';

const STATUS_CONFIG: Record<BookingStatus, { label: string; className: string }> = {
  Pending: { label: 'Chờ xác nhận', className: 'bg-amber-400/15 text-amber-600 border-amber-400/30' },
  Confirmed: { label: 'Đã xác nhận', className: 'bg-sage-100 text-sage-700 border-sage-300' },
  Completed: { label: 'Hoàn thành', className: 'bg-stone-100 text-ink-light border-stone-200' },
  Cancelled: { label: 'Đã hủy', className: 'bg-rose-50 text-rose-500 border-rose-500/30' },
};

export function StatusBadge({ status }: { status: BookingStatus }) {
  const config = STATUS_CONFIG[status];

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium ${config.className}`}
    >
      {config.label}
    </span>
  );
}
