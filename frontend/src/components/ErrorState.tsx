interface ErrorStateProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-rose-500/20 bg-rose-50 py-16 text-center">
      <p className="max-w-sm text-sm text-rose-500">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="btn-secondary">
          Thử lại
        </button>
      )}
    </div>
  );
}
