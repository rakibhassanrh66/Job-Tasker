// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { CalendarClock, Files, Inbox } from "lucide-react";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { AssignmentStatusBadge } from "@/components/status-badge";
import { Card, PageHeader, SelectField, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast } from "@/lib/format";
import { useApiPagedQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import { AssignmentStatus, assignmentStatusLabels, type AssignmentDto, type PagedResult } from "@/lib/types";

const PAGE_SIZE = 10;

export default function AdminAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data } = useApiPagedQuery<AssignmentDto>(
    ["admin", "assignments"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined },
    () =>
      api.get<PagedResult<AssignmentDto>>(
        `/assignments${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch, status: status || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

const publishedCount = rows.filter((row) => row.status === AssignmentStatus.Published).length;
const draftCount = rows.filter((row) => row.status === AssignmentStatus.Draft).length;

  const columns: SortableColumn<AssignmentDto>[] = [
    {
      key: "title",
      header: "Assignment",
      sortValue: (row) => row.title.toLowerCase(),
      render: (row) => (
        <div>
          <p className="font-semibold text-white">{row.title}</p>
          <p className="mt-0.5 text-xs text-slate-500">
            {row.subjectName} · <span className="font-mono">{row.classCourseCode}</span>
          </p>
        </div>
      ),
    },
    {
      key: "createdByTeacherName",
      header: "Teacher",
      sortValue: (row) => row.createdByTeacherName.toLowerCase(),
      render: (row) => <p className="font-medium text-slate-300">{row.createdByTeacherName}</p>,
      hideBelow: "sm",
    },
    {
      key: "deadline",
      header: "Deadline",
      sortValue: (row) => row.deadline,
      render: (row) => (
        <span className={isPast(row.deadline) ? "text-slate-500" : "text-slate-200"}>
          {formatDateTime(row.deadline)}
        </span>
      ),
      hideBelow: "sm",
    },
    {
      key: "maxMarks",
      header: "Max",
      sortValue: (row) => row.maxMarks,
      render: (row) => <span className="font-mono text-slate-300">{row.maxMarks}</span>,
    },
    {
      key: "submissionCount",
      header: "Subs",
      sortValue: (row) => row.submissionCount,
      render: (row) => (
        <span className="inline-flex items-center gap-1.5 font-mono text-slate-300">
          <Inbox className="h-3.5 w-3.5 text-slate-500" aria-hidden />
          {row.submissionCount}
        </span>
      ),
    },
    {
      key: "status",
      header: "Status",
      sortValue: (row) => assignmentStatusLabels[row.status],
      render: (row) => <AssignmentStatusBadge status={row.status} />,
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="All Assignments"
        description="Read-only oversight of everything published across the institution."
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Total Assignments" value={totalCount} tone="amber" icon={Files} />
        <StatCard title="Published (this page)" value={publishedCount} tone="emerald" />
        <StatCard title="Drafts (this page)" value={draftCount} tone="slate" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <div className="w-full max-w-md">
            <TextField
              label="Search Assignments"
              placeholder="Search by title, subject or teacher…"
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
          emptyHint="Adjust the filters, or wait for teachers to publish new work."
          emptyIcon={CalendarClock}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>
    </div>
  );
}