// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import { Card, PageHeader, SelectField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import {
  SubmissionStatus,
  submissionStatusLabels,
  type PagedResult,
  type SubmissionDto,
} from "@/lib/types";
import { useApi } from "@/lib/use-api";

/** Every submission in the system. Read-only, for the same reason as the assignments
 *  oversight list: marking is the teacher's job and rule 4 governs it. */
export default function AdminSubmissionsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");

  const { data, error, loading } = useApi<PagedResult<SubmissionDto>>(
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/submissions${query({ page, pageSize: 10, status: status || undefined })}`,
      ),
    [page, status],
  );

  const columns: Column<SubmissionDto>[] = [
    {
      header: "Student",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.studentName}</p>
          <p className="text-xs text-slate-500">{row.studentEmail}</p>
        </div>
      ),
    },
    { header: "Assignment", cell: (row) => row.assignmentTitle },
    { header: "Submitted", secondary: true, cell: (row) => formatDateTime(row.submittedAt) },
    { header: "Status", cell: (row) => <SubmissionStatusBadge status={row.status} /> },
    {
      header: "Marks",
      align: "right",
      cell: (row) =>
        row.marks === null ? (
          <span className="text-slate-400">—</span>
        ) : (
          <span className="font-medium">
            {row.marks} / {row.maxMarks}
          </span>
        ),
    },
    { header: "Marked by", secondary: true, cell: (row) => row.gradedByTeacherName ?? "—" },
  ];

  return (
    <>
      <PageHeader
        title="All submissions"
        description="Every submission in the system, for oversight. Read-only."
      />

      <Card className="space-y-4">
        <div className="max-w-xs">
          <SelectField
            label="Status"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All statuses</option>
            {Object.values(SubmissionStatus).map((value) => (
              <option key={value} value={value}>
                {submissionStatusLabels[value]}
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
          empty="Nothing submitted yet"
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
