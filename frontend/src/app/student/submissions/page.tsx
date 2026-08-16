// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ArrowRight, GraduationCap } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import { Card, PageHeader, SelectField, StatCard } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { useApiPagedQuery } from "@/lib/query";
import {
  SubmissionStatus,
  submissionStatusLabels,
  type PagedResult,
  type SubmissionDto,
} from "@/lib/types";

const PAGE_SIZE = 10;

export default function StudentSubmissionsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");

  const { data } = useApiPagedQuery<SubmissionDto>(
    ["student", "submissions"],
    { page, pageSize: PAGE_SIZE, status: status || undefined },
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/submissions/mine${query({ page, pageSize: PAGE_SIZE, status: status || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const gradedCount = rows.filter((row) => row.marks !== null).length;
  const pendingCount = rows.filter((row) => row.marks === null).length;

  const columns: SortableColumn<SubmissionDto>[] = [
    {
      key: "assignmentTitle",
      header: "Assignment",
      sortValue: (row) => row.assignmentTitle.toLowerCase(),
      render: (row) => (
        <Link
          href={`/student/assignments/${row.assignmentId}`}
          className="font-semibold text-white transition-colors duration-150 hover:text-accent-300"
        >
          {row.assignmentTitle}
        </Link>
      ),
    },
    {
      key: "submittedAt",
      header: "Submitted",
      sortValue: (row) => row.submittedAt,
      render: (row) => formatDateTime(row.submittedAt),
      hideBelow: "sm",
    },
    {
      key: "status",
      header: "Status",
      sortValue: (row) => submissionStatusLabels[row.status],
      render: (row) => <SubmissionStatusBadge status={row.status} />,
    },
    {
      key: "marks",
      header: "Score",
      sortValue: (row) => row.marks ?? -1,
      render: (row) =>
        row.marks === null ? (
          <span className="font-mono text-xs text-slate-500">Awaiting grade</span>
        ) : (
          <span className="font-bold text-emerald-300">
            {row.marks} <span className="text-xs font-normal text-slate-500">/ {row.maxMarks}</span>
          </span>
        ),
    },
    {
      key: "action",
      header: "",
      className: "text-right",
      render: (row) => (
        <Link
          href={`/student/assignments/${row.assignmentId}`}
          className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-accent-400 transition-colors duration-150 hover:text-accent-300"
        >
          Open
          <ArrowRight className="h-3 w-3" aria-hidden />
        </Link>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Student"
        title="My Submissions"
        description="Everything you have handed in, with the current status and any marks returned."
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Total Submissions" value={totalCount} tone="amber" icon={GraduationCap} />
        <StatCard title="Graded (this page)" value={gradedCount} tone="emerald" />
        <StatCard title="Awaiting Grade (this page)" value={pendingCount} tone="sky" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
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
            {Object.values(SubmissionStatus).map((value) => (
              <option key={value} value={value}>
                {submissionStatusLabels[value]}
              </option>
            ))}
          </SelectField>
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No submissions yet"
          emptyHint="Hand something in from the available assignments list to see it here."
          emptyIcon={GraduationCap}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>
    </div>
  );
}