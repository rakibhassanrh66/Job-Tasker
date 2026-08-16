// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";

/**
 * Server-side pager. The API counts rows, so page numbers are driven entirely by
 * `totalCount` / `pageSize`; the page button set adapts around the current page.
 */

function pageWindow(current: number, total: number): (number | "gap")[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages: (number | "gap")[] = [1];

  if (current > 4) {
    pages.push("gap");
  }

  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }

  if (current < total - 3) {
    pages.push("gap");
  }

  pages.push(total);

  return pages;
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  if (totalPages <= 1) {
    return null;
  }

  const pageButtonClass = (active: boolean) =>
    `flex h-9 min-w-9 items-center justify-center rounded-md border px-2.5 text-sm font-semibold transition-all duration-150 ${
      active
        ? "border-accent-400 bg-accent-400 text-ink-950"
        : "border-line bg-ink-850 text-slate-400 hover:border-ink-500 hover:text-white"
    }`;

  return (
    <nav aria-label="Pagination" className="flex flex-wrap items-center justify-between gap-3">
      <p className="text-xs text-slate-500">
        {totalCount.toLocaleString()} result{totalCount === 1 ? "" : "s"} · page {page} of{" "}
        {totalPages.toLocaleString()}
      </p>

      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
          className={`${pageButtonClass(false)} disabled:cursor-not-allowed disabled:opacity-40`}
        >
          <ChevronLeft className="h-4 w-4" aria-hidden />
        </button>

        {pageWindow(page, totalPages).map((p, index) =>
          p === "gap" ? (
            <span
              key={`gap-${index}`}
              className="flex h-9 min-w-5 items-center justify-center text-sm text-slate-500"
            >
              …
            </span>
          ) : (
            <button
              key={p}
              type="button"
              onClick={() => onPageChange(p)}
              aria-current={p === page ? "page" : undefined}
              className={pageButtonClass(p === page)}
            >
              {p}
            </button>
          ),
        )}

        <button
          type="button"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
          className={`${pageButtonClass(false)} disabled:cursor-not-allowed disabled:opacity-40`}
        >
          <ChevronRight className="h-4 w-4" aria-hidden />
        </button>
      </div>
    </nav>
  );
}