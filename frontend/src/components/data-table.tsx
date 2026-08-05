// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import type { ReactNode } from "react";
import { EmptyState, ErrorBanner, Spinner } from "./ui";

export interface Column<T> {
  header: string;
  cell: (row: T) => ReactNode;
  /** Hidden below the `sm` breakpoint. Used for columns that are useful but not essential,
   *  so a phone shows a readable table rather than a horizontally cramped one. */
  secondary?: boolean;
  align?: "left" | "right";
}

/**
 * One table for every list in the app.
 *
 * Also owns the loading, error and empty states, because every list needs all four and
 * repeating them per screen is where they drift apart.
 */
export function DataTable<T>({
  rows,
  columns,
  loading,
  error,
  empty,
  emptyHint,
  rowKey,
}: {
  rows: T[] | undefined;
  columns: Column<T>[];
  loading?: boolean;
  error?: unknown;
  empty: string;
  emptyHint?: string;
  rowKey: (row: T) => string;
}) {
  if (error) {
    return <ErrorBanner error={error} />;
  }

  if (loading && !rows) {
    return (
      <div className="flex justify-center py-10">
        <Spinner />
      </div>
    );
  }

  if (!rows || rows.length === 0) {
    return <EmptyState title={empty} hint={emptyHint} />;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left dark:border-slate-700">
            {columns.map((column) => (
              <th
                key={column.header}
                scope="col"
                className={`px-3 py-2.5 font-medium text-slate-600 dark:text-slate-400 ${
                  column.secondary ? "hidden sm:table-cell" : ""
                } ${column.align === "right" ? "text-right" : ""}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>

        <tbody>
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              className="border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              {columns.map((column) => (
                <td
                  key={column.header}
                  className={`px-3 py-3 align-top text-slate-800 dark:text-slate-200 ${
                    column.secondary ? "hidden sm:table-cell" : ""
                  } ${column.align === "right" ? "text-right" : ""}`}
                >
                  {column.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
