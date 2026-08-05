// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { AssignmentStatusBadge } from "@/components/status-badge";
import { Card, PageHeader, SelectField, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast } from "@/lib/format";
import {
  AssignmentStatus,
  assignmentStatusLabels,
  type AssignmentDto,
  type PagedResult,
} from "@/lib/types";
import { useApi } from "@/lib/use-api";

/**
 * Every assignment in the system, at any status.
 *
 * Read-only. An admin oversees the system rather than teaching in it, and editing another
 * teacher's work would cut across business rule 4 — the API would refuse it anyway.
 */
export default function AdminAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");

  const { data, error, loading } = useApi<PagedResult<AssignmentDto>>(
    () =>
      api.get<PagedResult<AssignmentDto>>(
        `/assignments${query({ page, pageSize: 10, search, status: status || undefined })}`,
      ),
    [page, search, status],
  );

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
    { header: "Teacher", secondary: true, cell: (row) => row.createdByTeacherName },
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
    { header: "Submissions", align: "right", cell: (row) => row.submissionCount },
  ];

  return (
    <>
      <PageHeader
        title="All assignments"
        description="Every assignment in the system, for oversight. Read-only."
      />

      <Card className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2 lg:max-w-xl">
          <TextField
            label="Search"
            placeholder="Filter by title"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <SelectField
            label="Status"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
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
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
