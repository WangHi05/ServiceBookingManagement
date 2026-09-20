interface PaginationProps {
  pageNumber: number;
  totalPages: number;
  onChange: (page: number) => void;
}

export function Pagination({ pageNumber, totalPages, onChange }: PaginationProps) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-center gap-2 pt-6">
      <button
        className="btn-secondary px-3 py-2"
        disabled={pageNumber <= 1}
        onClick={() => onChange(pageNumber - 1)}
      >
        Trước
      </button>

      <span className="px-3 text-sm text-ink-light">
        Trang {pageNumber} / {totalPages}
      </span>

      <button
        className="btn-secondary px-3 py-2"
        disabled={pageNumber >= totalPages}
        onClick={() => onChange(pageNumber + 1)}
      >
        Sau
      </button>
    </div>
  );
}
