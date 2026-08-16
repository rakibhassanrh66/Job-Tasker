// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { GraduationCap, Inbox } from "lucide-react";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import { Card, PageHeader, SelectField, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { useApiPagedQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import {
  SubmissionStatus,
  submissionStatusLabels,
  type PagedResult,
  type SubmissionDto,
} from "@/lib/types";

const PAGE_SIZE = 10;

export default function AdminSubmissionsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data } = useApiPagedQuery<SubmissionDto>(
    ["admin", "submissions"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined },
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/submissions${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const gradedCount = rows.filter((row) => row.status === SubmissionStatus.Graded).length;
  const pendingCount = rows.filter(
    (row) => row.status === SubmissionStatus.Submitted || row.status === SubmissionStatus.Late,
  ).length;
  const underReviewCount = rows.filter((row) => row.status === SubmissionStatus.UnderReview).length;

  const columns: SortableColumn<SubmissionDto>[] = [
    {
      key: "studentName",
      header: "Student",
      sortValue: (row) => row.studentName.toLowerCase(),
      render: (row) => (
        <div>
          <p className="font-semibold text-white">{row.studentName}</p>
          <p className="mt-0.5 truncate font-mono text-xs text-slate-500">{row.studentEmail}</p>
        </div>
      ),
    },
    {
      key: "assignmentTitle",
      header: "Assignment",
      sortValue: (row) => row.assignmentTitle.toLowerCase(),
      render: (row) => <p className="max-w-56 truncate font-medium text-slate-300">{row.assignmentTitle}</p>,
    },
    {
      key: "status",
      header: "Status",
      sortValue: (row) => submissionStatusLabels[row.status],
      render: (row) => <SubmissionStatusBadge status={row.status} />,
    },
    {
      key: "marks",
      header: "Result",
      sortValue: (row) => row.marks ?? -1,
      render: (row) =>
        row.marks === null ? (
          <span className="text-slate-500">—</span>
        ) : (
          <span className="font-mono font-semibold text-emerald-300">
            {row.marks} / {row.maxMarks}
          </span>
        ),
    },
    {
      key: "submittedAt",
      header: "Submitted",
      sortValue: (row) => row.submittedAt,
      render: (row) => formatDateTime(row.submittedAt),
      hideBelow: "sm",
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="All Submissions"
        description="The institution-wide submission stream: who handed in what, and how it graded."
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Total Submissions" value={totalCount} tone="amber" icon={GraduationCap} />
        <StatCard title="Awaiting Review (this page)" value={pendingCount} tone="sky" />
        <StatCard title="Under Review (this page)" value={underReviewCount} tone="violet" />
        <StatCard title="Graded (this page)" value={gradedCount} tone="emerald" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <div className="w-full max-w-md">
            <TextField
              label="Search Submissions"
              placeholder="Search by student or assignment…"
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
              {Object.values(SubmissionStatus).map((value) => (
                <option key={value} value={value}>
                  {submissionStatusLabels[value]}
                </option>
              ))}
            </SelectField>
          </div>
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No submissions found"
          emptyHint="Adjust the filters, or check back once students start submitting."
          emptyIcon={Inbox}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>
    </div>
  );
}