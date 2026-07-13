import { ChevronLeft, ChevronRight } from 'lucide-react';

interface PaginationProps {
  limit: number;
  offset: number;
  totalCount: number;
  onOffsetChange: (offset: number) => void;
}

export function Pagination({ limit, offset, totalCount, onOffsetChange }: PaginationProps) {
  if (totalCount === 0) {
    return null;
  }

  const start = offset + 1;
  const end = Math.min(offset + limit, totalCount);
  const currentPage = Math.floor(offset / limit) + 1;
  const pageCount = Math.max(1, Math.ceil(totalCount / limit));

  return (
    <nav className="pagination" aria-label="Table pagination">
      <p>
        <span className="pagination__range">{start}-{end}</span> of {totalCount}
      </p>
      <div>
        <button
          className="icon-button"
          type="button"
          title="Previous page"
          aria-label="Previous page"
          disabled={offset === 0}
          onClick={() => onOffsetChange(Math.max(0, offset - limit))}
        >
          <ChevronLeft aria-hidden="true" size={18} />
        </button>
        <span aria-current="page">
          Page {currentPage} of {pageCount}
        </span>
        <button
          className="icon-button"
          type="button"
          title="Next page"
          aria-label="Next page"
          disabled={end >= totalCount}
          onClick={() => onOffsetChange(offset + limit)}
        >
          <ChevronRight aria-hidden="true" size={18} />
        </button>
      </div>
    </nav>
  );
}
