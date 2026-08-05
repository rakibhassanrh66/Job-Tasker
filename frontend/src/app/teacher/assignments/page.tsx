// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
import { useState } from "react";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { AssignmentStatusBadge } from "@/components/status-badge";
import { Button, Card, ErrorBanner, PageHeader, SelectField, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast } from "@/lib/format";
import {
  AssignmentStatus,
  assignmentStatusLabels,
  type AssignmentDto,
  type PagedResult,
} from "@/lib/types";
import { useApi } from "@/lib/use-api";

export default function TeacherAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [failure, setFailure] = useState<unknown>(null);

  const { data, error, loading, reload } = useApi<PagedResult<AssignmentDto>>(
    () =>
      api.get<PagedResult<AssignmentDto>>(
        `/assignments/mine${query({ page, pageSize: 10, search, status: status || undefined })}`,
      ),
    [page, search, status],
  );

  const act = async (id: string, action: () => Promise<unknown>) => {
    setBusy(id);
    setFailure(null);

    try {
      await action();
      reload();
    } catch (cause) {
      setFailure(cause);
    } finally {
      setBusy(null);
    }
  };

  const publish = (row: AssignmentDto) =>
    act(row.id, () => api.post(`/assignments/${row.id}/publish`));

  const remove = (row: AssignmentDto) => {
    // The API refuses to delete an assignment that has submissions — deleting would take
    // students' work with it — so warn before spending the request.
    const warning =
      row.submissionCount > 0
        ? `"${row.title}" has ${row.submissionCount} submission(s) and cannot be deleted. Try anyway?`
        : `Delete "${row.title}"? This cannot be undone.`;

    if (window.confirm(warning)) {
      void act(row.id, () => api.del(`/assignments/${row.id}`));
    }
  };

  const columns: Column<AssignmentDto>[] = [
    {
      header: "Assignment",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.title}</p>
          <p className="mt-0.5 text-xs text-slate-500">
            {row.subjectName} · {row.classCourseCode}
          </p>
        </div>
      ),
    },
    {
      header: "Deadline",
      secondary: true,
      cell: (row) => (
        <span className={isPast(row.deadline) ? "text-red-600" : undefined}>
          {formatDateTime(row.deadline)}
        </span>
      ),
    },
    { header: "Status", cell: (row) => <AssignmentStatusBadge status={row.status} /> },
    {
      header: "Submissions",
      align: "right",
      cell: (row) => (
        <Link
          href={`/teacher/assignments/${row.id}/submissions`}
          className="font-medium underline-offset-2 hover:underline"
        >
          {row.submissionCount}
        </Link>
      ),
    },
    {
      header: "Actions",
      align: "right",
      cell: (row) => (
        <div className="flex flex-wrap justify-end gap-1.5">
          {row.status === AssignmentStatus.Draft && (
            <Button variant="secondary" disabled={busy === row.id} onClick={() => void publish(row)}>
              Publish
            </Button>
          )}
          <Link href={`/teacher/assignments/${row.id}`}>
            <Button variant="secondary">Edit</Button>
          </Link>
          <Button variant="ghost" disabled={busy === row.id} onClick={() => remove(row)}>
            Delete
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="My assignments"
        description="Work you have created, at any status."
        action={
          <Link href="/teacher/assignments/new">
            <Button>New assignment</Button>
          </Link>
        }
      />

      <ErrorBanner error={failure} />

      <Card className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2 lg:max-w-xl">
          <TextField
            label="Search"
            placeholder="Filter by title"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />

          <SelectField
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All statuses</option>
            {Object.values(AssignmentStatus).map((value) => (
              <option key={value} value={value}>
                {assignmentStatusLabels[value]}
              </option>
            ))}
          </SelectField>
        </div>

        <DataTable
          rows={data?.items}
          columns={columns}
          loading={loading}
          error={error}
          rowKey={(row) => row.id}
          empty="No assignments yet"
          emptyHint="Create one to get started. It stays a draft until you publish it."
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
