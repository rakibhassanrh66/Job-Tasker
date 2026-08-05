// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
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

/**
 * The student's own work, with marks and feedback.
 *
 * Only `assignmentId` and `status` are sent — this route's query type has no studentId,
 * because the caller's own id is the only one it could ever mean.
 */
export default function StudentSubmissionsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>("");

  const { data, error, loading } = useApi<PagedResult<SubmissionDto>>(
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/submissions/mine${query({ page, pageSize: 10, status: status || undefined })}`,
      ),
    [page, status],
  );

  const columns: Column<SubmissionDto>[] = [
    {
      header: "Assignment",
      cell: (row) => (
        <Link
          href={`/student/assignments/${row.assignmentId}`}
          className="font-medium text-slate-900 underline-offset-2 hover:underline dark:text-white"
        >
          {row.assignmentTitle}
        </Link>
      ),
    },
    {
      header: "Submitted",
      secondary: true,
      cell: (row) => formatDateTime(row.submittedAt),
    },
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
    {
      header: "Feedback",
      secondary: true,
      cell: (row) =>
        row.feedback ? (
          <span className="text-slate-700 dark:text-slate-300">{row.feedback}</span>
        ) : (
          <span className="text-slate-400">—</span>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        title="My submissions"
        description="Everything you have handed in, with marks and teacher feedback."
      />

      <Card className="space-y-4">
        <div className="max-w-xs">
          <SelectField
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
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
          emptyHint="Your submissions appear here once you hand work in."
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
