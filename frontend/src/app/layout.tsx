import type { Metadata } from 'next';
import { Fraunces, Inter } from 'next/font/google';
import { AuthProvider } from '@/context/AuthContext';
import { Navbar } from '@/components/Navbar';
import './globals.css';

const fraunces = Fraunces({
  subsets: ['latin'],
  variable: '--font-fraunces',
  weight: ['500', '600'],
});

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
});

export const metadata: Metadata = {
  title: 'Service Booking Salon',
  description: 'Đặt lịch dịch vụ salon trực tuyến',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi" className={`${fraunces.variable} ${inter.variable}`}>
      <body>
        <AuthProvider>
          <Navbar />
          <main className="mx-auto max-w-5xl px-6 py-8">{children}</main>
        </AuthProvider>
      </body>
    </html>
  );
}
