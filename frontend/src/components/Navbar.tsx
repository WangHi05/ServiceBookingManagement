'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import clsx from 'clsx';
import { useAuth } from '@/context/AuthContext';

const CUSTOMER_LINKS = [
  { href: '/services', label: 'Dịch vụ' },
  { href: '/my-bookings', label: 'Lịch của tôi' },
];

const ADMIN_LINKS = [
  { href: '/admin/services', label: 'Dịch vụ' },
  { href: '/admin/schedules', label: 'Lịch làm việc' },
  { href: '/admin/bookings', label: 'Booking' },
];

export function Navbar() {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  if (!user) return null;

  const navLinks = user.role === 'Admin' ? ADMIN_LINKS : CUSTOMER_LINKS;
  const homeHref = user.role === 'Admin' ? '/admin/services' : '/services';

  return (
    <header className="border-b border-stone-200 bg-white">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
        <Link href={homeHref} className="font-display text-lg font-semibold text-ink">
          Booking<span className="text-sage-600">Salon</span>
        </Link>

        <nav className="flex items-center gap-1">
          {navLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={clsx(
                'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                pathname.startsWith(link.href)
                  ? 'bg-sage-50 text-sage-700'
                  : 'text-ink-light hover:bg-stone-100'
              )}
            >
              {link.label}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <span className="text-sm text-ink-light">
            {user.fullName}
            {user.role === 'Admin' && (
              <span className="ml-2 rounded-full bg-amber-400/15 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-amber-600">
                Admin
              </span>
            )}
          </span>
          <button onClick={logout} className="btn-secondary px-3 py-2 text-xs">
            Đăng xuất
          </button>
        </div>
      </div>
    </header>
  );
}
