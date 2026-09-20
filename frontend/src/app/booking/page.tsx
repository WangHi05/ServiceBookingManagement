import { Suspense } from 'react';
import { BookingForm } from './BookingForm';
import { LoadingState } from '@/components/LoadingState';

export default function BookingPage() {
  return (
    <div>
      <div className="flex flex-col gap-1">
        <h1 className="font-display text-2xl font-semibold text-ink">Đặt lịch</h1>
        <p className="text-sm text-ink-light">
          Chọn dịch vụ, ngày và khung giờ còn trống, sau đó xác nhận.
        </p>
      </div>

      <div className="mt-6">
        <Suspense fallback={<LoadingState />}>
          <BookingForm />
        </Suspense>
      </div>
    </div>
  );
}
