// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import type { PagedResult } from "@/lib/types";
import { Button } from "./ui";

/**
 * Bound to the API's PagedResult, which already carries hasPrevious/hasNext/totalPages —
 * so the client never recomputes what the server has said, and the buttons cannot disagree
 * with the data.
 */
export function Pagination<T>({
  page,
  onPageChange,
}: {
  page: PagedResult<T>;
  onPageChange: (next: number) => void;
}) {
  if (page.totalPages <= 1) {
    return null;
  }

  const first = (page.page - 1) * page.pageSize + 1;
  const last = Math.min(page.page * page.pageSize, page.totalCount);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-3 dark:border-slate-700">
      <p className="text-sm text-slate-600 dark:text-slate-400">
        {first}–{last} of {page.totalCount}
      </p>

      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          disabled={!page.hasPrevious}
          onClick={() => onPageChange(page.page - 1)}
        >
          Previous
        </Button>

        <span className="text-sm text-slate-600 dark:text-slate-400">
          Page {page.page} of {page.totalPages}
        </span>

        <Button
          variant="secondary"
          disabled={!page.hasNext}
          onClick={() => onPageChange(page.page + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}
