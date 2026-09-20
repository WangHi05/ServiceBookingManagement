export function LoadingState({ label = 'Đang tải dữ liệu…' }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20 text-ink-light">
      <span className="h-6 w-6 animate-spin rounded-full border-2 border-sage-300 border-t-sage-600" />
      <p className="text-sm">{label}</p>
    </div>
  );
}
