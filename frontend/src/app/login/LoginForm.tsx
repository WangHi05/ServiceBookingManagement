'use client';

import { FormEvent, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { getErrorMessage } from '@/lib/api-client';
import type { User } from '@/lib/types';

export function LoginForm() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    // Chỉ bọc try/catch quanh chính lời gọi login(): nếu để router.push/refresh
    // trong cùng khối try, một lỗi điều hướng (khác hẳn bản chất "sai email/mật khẩu")
    // sẽ bị bắt nhầm và hiển thị như thể đăng nhập thất bại — trong khi thực tế
    // tài khoản đã đăng nhập thành công (Navbar đã đổi), chỉ là chuyển trang bị lỗi.
    let loggedInUser: User;
    try {
      loggedInUser = await login(email, password);
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng nhập thất bại. Vui lòng thử lại.'));
      setIsSubmitting(false);
      return;
    }

    const defaultRoute = loggedInUser.role === 'Admin' ? '/admin/services' : '/services';
    const redirectTo = searchParams.get('redirectTo') || defaultRoute;
    router.push(redirectTo);
    // Không gọi router.refresh() ở đây: router.push() sang route mới đã tự fetch
    // dữ liệu mới cho route đích rồi; gọi refresh() ngay sau đó từng gây race điều
    // hướng (đứng lại ở /login, đôi khi kèm lỗi ngầm) — đây là nguyên nhân Lỗi 1.
  }

  return (
    <div className="flex min-h-[80vh] items-center justify-center">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <h1 className="font-display text-2xl font-semibold text-ink">
            Booking<span className="text-sage-600">Salon</span>
          </h1>
          <p className="mt-2 text-sm text-ink-light">Đăng nhập để đặt lịch dịch vụ</p>
        </div>

        <form onSubmit={handleSubmit} className="card rounded-lg p-6 shadow-sm" noValidate>
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-ink">
              Email
            </label>
            <input
              id="email"
              type="email"
              required
              autoComplete="email"
              className="input-field mt-1.5"
              placeholder="ban@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="mt-4">
            <label htmlFor="password" className="block text-sm font-medium text-ink">
              Mật khẩu
            </label>
            <input
              id="password"
              type="password"
              required
              autoComplete="current-password"
              className="input-field mt-1.5"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          {error && (
            <p className="mt-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-500" role="alert">
              {error}
            </p>
          )}

          <button type="submit" className="btn-primary mt-6 w-full" disabled={isSubmitting}>
            {isSubmitting ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </button>
        </form>

      </div>
    </div>
  );
}
