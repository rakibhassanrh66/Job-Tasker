// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { motion, type Variants } from "motion/react";
import { ArrowDown, ArrowUp, ArrowUpDown, type LucideIcon } from "lucide-react";
import React, { useMemo, useState, type ReactNode } from "react";
import { EmptyState, Skeleton } from "@/components/ui";
import { usePrefersReducedMotion } from "@/lib/use-prefers-reduced-motion";

/**
 * Staggered row reveal: the tbody staggers its rows 0.05s apart, each rising
 * (y 20 → 0, opacity 0 → 1) over 0.4s with the system easing. Defined as module-level
 * variants so their identity is stable across renders.
 */
const tableVariants: Variants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.05 },
  },
};

const rowVariants: Variants = {
  hidden: { opacity: 0, y: 20 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] },
  },
};

/**
 * SortableDataTable.
 *
 * The backend list endpoints deliberately reject unknown query parameters (only
 * page/pageSize/search/status/role/teacherId are legal), so sorting is always done
 * client-side on the page the server already returned. Column headers cycle
 * asc → desc → none. Row cells are flex/grid columns, so a row can be a summary line
 * with the action buttons right-aligned.
 */

export interface SortableColumn<T> {
  key: string;
  header: string;
  /** Value used for sorting. Falls back to the rendered node when omitted. */
  sortValue?: (row: T) => string | number;
  /** Renderer for the cell; default is the raw value. */
  render?: (row: T) => ReactNode;
  /** Hide the cell below sm screens. */
  hideBelow?: "sm";
  className?: string;
}

export function SortableDataTable<T extends { id: string | number }>({
  columns,
  rows,
  rowKey = (row) => row.id,
  onRowClick,
  loading = false,
  emptyTitle = "Nothing here yet",
  emptyHint = "No records match the current view.",
  emptyAction,
  emptyIcon,
}: {
  columns: SortableColumn<T>[];
  rows: T[];
  rowKey?: (row: T) => string | number;
  onRowClick?: (row: T) => void;
  loading?: boolean;
  emptyTitle?: string;
  emptyHint?: string;
  emptyAction?: ReactNode;
  emptyIcon?: LucideIcon;
}) {
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");
  const reducedMotion = usePrefersReducedMotion();

  const sorted = useMemo(() => {
    if (!sortKey) {
      return rows;
    }

    const column = columns.find((c) => c.key === sortKey);

    if (!column) {
      return rows;
    }

    const next = [...rows];

    next.sort((a, b) => {
      const av = column.sortValue ? column.sortValue(a) : String(a[sortKey as keyof T]);
      const bv = column.sortValue ? column.sortValue(b) : String(b[sortKey as keyof T]);

      const comparison =
        typeof av === "number" && typeof bv === "number"
          ? av - bv
          : String(av).localeCompare(String(bv), undefined, { numeric: true });

      return sortDirection === "asc" ? comparison : -comparison;
    });

    return next;
  }, [columns, rows, sortKey, sortDirection]);

  const toggleSort = (key: string) => {
    if (sortKey !== key) {
      setSortKey(key);
      setSortDirection("asc");
    } else if (sortDirection === "asc") {
      setSortDirection("desc");
    } else {
      setSortKey(null);
    }
  };

  const SortIcon = ({ column }: { column: SortableColumn<T> }) => {
    if (sortKey !== column.key) {
      return <ArrowUpDown className="ml-1 inline-block h-3 w-3 opacity-40" aria-hidden />;
    }

    return sortDirection === "asc" ? (
      <ArrowUp className="ml-1 inline-block h-3 w-3 text-accent-400" aria-hidden />
    ) : (
      <ArrowDown className="ml-1 inline-block h-3 w-3 text-accent-400" aria-hidden />
    );
  };

  if (loading) {
    return (
      <div className="overflow-hidden rounded-md border border-line bg-ink-900/70">
        <div className="border-b border-line px-4 py-3">
          <Skeleton className="h-4 w-40" />
        </div>
        <div className="space-y-0">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              className="flex items-center justify-between gap-4 border-b border-line/60 px-4 py-4"
            >
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-4 w-20" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <EmptyState title={emptyTitle} hint={emptyHint} action={emptyAction} icon={emptyIcon} />
    );
  }

  return (
    <div className="overflow-hidden rounded-md border border-line bg-ink-900/70">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line">
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={`px-4 py-3 ${column.hideBelow === "sm" ? "hidden sm:table-cell" : ""} ${
                    column.className ?? ""
                  }`}
                >
                  <button
                    type="button"
                    onClick={() => toggleSort(column.key)}
                    className="inline-flex cursor-pointer items-center gap-1 text-[11px] font-bold uppercase tracking-widest text-slate-500 transition-colors duration-150 hover:text-slate-200"
                  >
                    {column.header}
                    <SortIcon column={column} />
                  </button>
                </th>
              ))}
            </tr>
          </thead>
          {reducedMotion ? (
            <tbody>
              {sorted.map((row) => (
                <tr
                  key={rowKey(row)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={`border-b border-line/50 transition-colors duration-150 last:border-b-0 ${
                    onRowClick ? "cursor-pointer hover:bg-ink-800/60" : "hover:bg-ink-850/50"
                  }`}
                >
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={`px-4 py-3.5 align-middle ${column.hideBelow === "sm" ? "hidden sm:table-cell" : ""} ${
                        column.className ?? ""
                      }`}
                    >
                      {column.render ? column.render(row) : String(row[column.key as keyof T])}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          ) : (
            <motion.tbody variants={tableVariants} initial="hidden" animate="visible">
              {sorted.map((row) => (
                <motion.tr
                  key={rowKey(row)}
                  variants={rowVariants}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={`border-b border-line/50 transition-colors duration-150 last:border-b-0 ${
                    onRowClick ? "cursor-pointer hover:bg-ink-800/60" : "hover:bg-ink-850/50"
                  }`}
                >
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={`px-4 py-3.5 align-middle ${column.hideBelow === "sm" ? "hidden sm:table-cell" : ""} ${
                        column.className ?? ""
                      }`}
                    >
                      {column.render ? column.render(row) : String(row[column.key as keyof T])}
                    </td>
                  ))}
                </motion.tr>
              ))}
            </motion.tbody>
          )}
        </table>
      </div>
    </div>
  );
}