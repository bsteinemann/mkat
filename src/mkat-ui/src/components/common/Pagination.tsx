interface Props {
  page: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, totalCount, pageSize, onPageChange }: Props) {
  const totalPages = Math.ceil(totalCount / pageSize);

  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-between pt-4">
      <span className="text-sm text-gray-600">
        {totalCount} total items
      </span>
      <div className="flex gap-2">
        <button
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
        >
          Previous
        </button>
        <span className="px-3 py-1 text-sm">
          Page {page} of {totalPages}
        </span>
        <button
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
        >
          Next
        </button>
      </div>
    </div>
  );
}
