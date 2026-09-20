import { LoadingState } from '@/components/LoadingState';

export default function RootPage() {
  // Middleware (src/middleware.ts) luôn điều hướng "/" -> /services (đã đăng nhập)
  // hoặc -> /login (chưa đăng nhập) trước khi trang này kịp render.
  // Component này chỉ là fallback an toàn, hầu như không bao giờ hiển thị thực tế.
  return <LoadingState label="Đang chuyển hướng…" />;
}
