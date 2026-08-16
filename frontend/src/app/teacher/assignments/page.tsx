// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { Files, Inbox, Pencil, Plus, Trash2 } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { ConfirmDialog } from "@/components/modal";
import { Pagination } from "@/components/pagination";
import { AssignmentStatusBadge } from "@/components/status-badge";
import { Badge, Button, Card, PageHeader, SelectField, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast } from "@/lib/format";
import { useApiMutation, useApiPagedQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import {
  AssignmentStatus,
  assignmentStatusLabels,
  type AssignmentDto,
  type PagedResult,
} from "@/lib/types";

const PAGE_SIZE = 10;

export default function TeacherAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [pendingDelete, setPendingDelete] = useState<AssignmentDto | null>(null);

  const { data } = useApiPagedQuery<AssignmentDto>(
    ["teacher", "assignments"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined },
    () =>
      api.get<PagedResult<AssignmentDto>>(
        `/assignments/mine${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const publishedCount = rows.filter((row) => row.status === AssignmentStatus.Published).length;
  const draftCount = rows.filter((row) => row.status === AssignmentStatus.Draft).length;
  const totalSubmissions = rows.reduce((sum, row) => sum + row.submissionCount, 0);

  const publish = useApiMutation<unknown, AssignmentDto>({
    mutationFn: (row) => api.post(`/assignments/${row.id}/publish`),
    invalidate: [["teacher", "assignments"], ["teacher", "pending"]],
    successMessage: (_, row) => `"${row.title}" has been published.`,
  });

  const remove = useApiMutation<unknown, AssignmentDto>({
    mutationFn: (row) => api.del(`/assignments/${row.id}`),
    invalidate: [["teacher", "assignments"], ["teacher", "pending"]],
    successMessage: (_, row) => `Assignment "${row.title}" deleted.`,
  });

  const confirmDelete = () => {
    if (pendingDelete) {
      void remove.mutateAsync(pendingDelete);
    }

    setPendingDelete(null);
  };

  const columns: SortableColumn<AssignmentDto>[] = [
    {
      key: "title",
      header: "Assignment",
      sortValue: (row) => row.title.toLowerCase(),
      render: (row) => (
        <div>
          <Link
            href={`/teacher/assignments/${row.id}`}
            className="font-semibold text-white transition-colors duration-150 hover:text-accent-300"
          >
            {row.title}
          </Link>
          <div className="mt-1 flex items-center gap-1.5">
            <span className="text-xs text-slate-500">{row.subjectName}</span>
            <span className="text-slate-500">·</span>
            <Badge tone="blue">{row.classCourseCode}</Badge>
          </div>
        </div>
      ),
    },
    {
      key: "deadline",
      header: "Deadline",
      sortValue: (row) => row.deadline,
      render: (row) => {
        const expired = isPast(row.deadline);

        return (
          <div>
            <span
              className={`text-xs ${expired ? "font-semibold text-red-400" : "text-slate-400"}`}
            >
              {formatDateTime(row.deadline)}
            </span>
            <span
              className={`ml-2 text-[10px] font-bold uppercase tracking-wider ${
                expired ? "text-red-500" : "text-emerald-400"
              }`}
            >
              {expired ? "Expired" : "Active"}
            </span>
          </div>
        );
      },
      hideBelow: "sm",
    },
    {
      key: "status",
      header: "Status",
      sortValue: (row) => assignmentStatusLabels[row.status],
      render: (row) => <AssignmentStatusBadge status={row.status} />,
    },
    {
      key: "submissionCount",
      header: "Submissions",
      sortValue: (row) => row.submissionCount,
      render: (row) => (
        <Link
          href={`/teacher/assignments/${row.id}/submissions`}
          className="inline-flex items-center gap-1.5 rounded-md border border-line bg-ink-850 px-2.5 py-1 text-xs font-semibold text-slate-300 transition-colors duration-150 hover:border-ink-500 hover:text-white"
        >
          <Inbox className="h-3.5 w-3.5 text-accent-400" aria-hidden />
          {row.submissionCount}
          <span className="text-[11px] opacity-70">Review →</span>
        </Link>
      ),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => (
        <div className="flex flex-wrap justify-end gap-1.5">
          {row.status === AssignmentStatus.Draft && (
            <Button
              variant="success"
              size="sm"
              disabled={publish.isPending && publish.variables?.id === row.id}
              onClick={() => void publish.mutateAsync(row)}
            >
              Publish
            </Button>
          )}
          <Link href={`/teacher/assignments/${row.id}`}>
            <Button variant="secondary" size="sm">
              <Pencil className="h-3.5 w-3.5" aria-hidden />
              Edit
            </Button>
          </Link>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setPendingDelete(row)}
            disabled={remove.isPending && remove.variables?.id === row.id}
          >
            <Trash2 className="h-3.5 w-3.5 text-red-400" aria-hidden />
            Delete
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Teacher"
        title="My Assignments"
        description="Create coursework, publish drafts to enrolled classes, and evaluate submissions."
        action={
          <Link
            href="/teacher/assignments/new"
            className="inline-flex h-10 items-center gap-2 rounded-md bg-accent-400 px-4 text-sm font-semibold text-ink-950 transition-all duration-300 ease-expo hover:scale-[1.02] hover:bg-accent-300 hover:shadow-[0_0_24px_rgba(251,191,36,0.18)] active:scale-[0.99]"
          >
            <Plus className="h-4 w-4" aria-hidden />
            Create Assignment
          </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="My Assignments" value={totalCount} tone="amber" icon={Files} />
        <StatCard title="Published (this page)" value={publishedCount} tone="emerald" />
        <StatCard title="Drafts (this page)" value={draftCount} tone="slate" />
        <StatCard title="Total Submissions (this page)" value={totalSubmissions} tone="sky" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <div className="w-full max-w-md">
            <TextField
              label="Search Assignments"
              placeholder="Filter by assignment title…"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="w-full max-w-xs">
            <SelectField
              label="Filter by Status"
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All Statuses</option>
              {Object.values(AssignmentStatus).map((value) => (
                <option key={value} value={value}>
                  {assignmentStatusLabels[value]}
                </option>
              ))}
            </SelectField>
          </div>
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No assignments found"
          emptyHint="Create an assignment to get started. It stays a draft until you publish it."
          emptyIcon={Files}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <ConfirmDialog
        open={pendingDelete !== null}
        onClose={() => setPendingDelete(null)}
        onConfirm={confirmDelete}
        title="Delete assignment"
        message={
          pendingDelete && pendingDelete.submissionCount > 0
            ? `"${pendingDelete.title}" has ${pendingDelete.submissionCount} submission(s) and cannot be deleted while submissions exist.`
            : `Delete "${pendingDelete?.title ?? "this assignment"}"? This cannot be undone.`
        }
        confirmLabel="Delete"
      />
    </div>
  );
}