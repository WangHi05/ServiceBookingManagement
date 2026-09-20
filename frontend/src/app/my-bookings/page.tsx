import { Suspense } from 'react';
import { BookingsList } from './BookingsList';
import { LoadingState } from '@/components/LoadingState';

export default function MyBookingsPage() {
  return (
    <div>
      <div className="flex flex-col gap-1">
        <h1 className="font-display text-2xl font-semibold text-ink">Lịch của tôi</h1>
        <p className="text-sm text-ink-light">Xem, lọc và hủy lịch đặt của bạn.</p>
      </div>

      <div className="mt-6">
        <Suspense fallback={<LoadingState />}>
          <BookingsList />
        </Suspense>
      </div>
    </div>
  );
}
