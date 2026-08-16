// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ArrowRight, Files } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import { Badge, Card, PageHeader, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast, relativeToNow } from "@/lib/format";
import { useApiPagedQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import type { PagedResult, StudentAssignmentDto } from "@/lib/types";

const PAGE_SIZE = 10;

export default function StudentAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const { data } = useApiPagedQuery<StudentAssignmentDto>(
    ["student", "available"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch },
    () =>
      api.get<PagedResult<StudentAssignmentDto>>(
        `/assignments/available${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const submittedCount = rows.filter((row) => row.hasSubmitted).length;
  const pendingCount = rows.filter((row) => !row.hasSubmitted && !isPast(row.deadline)).length;
  const gradedCount = rows.filter((row) => row.marks !== null).length;

  const columns: SortableColumn<StudentAssignmentDto>[] = [
    {
      key: "title",
      header: "Assignment",
      sortValue: (row) => row.title.toLowerCase(),
      render: (row) => (
        <div>
          <Link
            href={`/student/assignments/${row.id}`}
            className="block font-semibold text-white transition-colors duration-150 hover:text-accent-300"
          >
            {row.title}
          </Link>
          <div className="mt-1 flex flex-wrap items-center gap-1.5 text-xs text-slate-500">
            <span>{row.subjectName}</span>
            <span>·</span>
            <Badge tone="blue">{row.classCourseCode}</Badge>
            <span>·</span>
            <span>Taught by {row.teacherName}</span>
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
              className={`text-xs ${expired ? "font-semibold text-red-400" : "font-medium text-slate-300"}`}
            >
              {formatDateTime(row.deadline)}
            </span>
            <span
              className={`block text-[11px] ${
                expired ? "font-bold text-red-500" : "text-emerald-400"
              }`}
            >
              {expired ? "Closed " : "Due "}
              {relativeToNow(row.deadline)}
            </span>
          </div>
        );
      },
      hideBelow: "sm",
    },
    {
      key: "submissionStatus",
      header: "My Status",
      sortValue: (row) => row.submissionStatus ?? 0,
      render: (row) =>
        row.hasSubmitted && row.submissionStatus ? (
          <SubmissionStatusBadge status={row.submissionStatus} />
        ) : isPast(row.deadline) && !row.allowLateSubmission ? (
          <Badge tone="red">Deadline Missed</Badge>
        ) : (
          <Badge tone="amber">Pending Submission</Badge>
        ),
    },
    {
      key: "marks",
      header: "Marks",
      sortValue: (row) => row.marks ?? -1,
      render: (row) =>
        row.marks === null ? (
          <span className="font-mono text-xs text-slate-500">—</span>
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
          href={`/student/assignments/${row.id}`}
          className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-accent-400 transition-colors duration-150 hover:text-accent-300"
        >
          {row.hasSubmitted ? "View Submission" : "Submit Answer"}
          <ArrowRight className="h-3 w-3" aria-hidden />
        </Link>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Student"
        title="Available Assignments"
        description="Published coursework across your enrolled classes. Submit work, and check the result once it is graded."
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Assigned Work" value={totalCount} tone="amber" icon={Files} />
        <StatCard title="Pending (this page)" value={pendingCount} tone="sky" />
        <StatCard title="Submitted (this page)" value={submittedCount} tone="emerald" />
        <StatCard title="Graded (this page)" value={gradedCount} tone="violet" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="w-full max-w-md">
          <TextField
            label="Search Coursework"
            placeholder="Search assignments by title…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No assignments published yet"
          emptyHint="Work assigned by your course instructors will appear here automatically."
          emptyIcon={Files}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>
    </div>
  );
}