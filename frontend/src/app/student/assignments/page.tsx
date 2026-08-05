// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
import { useState } from "react";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import { Badge, Card, PageHeader, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime, isPast, relativeToNow } from "@/lib/format";
import type { PagedResult, StudentAssignmentDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

/**
 * Published assignments for the classes this student is enrolled in.
 *
 * The list carries each row's own submission state, so "Submitted" and the mark render
 * without a request per row — and the search box sends only `search`, because this route
 * rejects a filter it cannot honour with 400 rather than ignoring it.
 */
export default function StudentAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");

  const { data, error, loading } = useApi<PagedResult<StudentAssignmentDto>>(
    () =>
      api.get<PagedResult<StudentAssignmentDto>>(
        `/assignments/available${query({ page, pageSize: 10, search })}`,
      ),
    [page, search],
  );

  const columns: Column<StudentAssignmentDto>[] = [
    {
      header: "Assignment",
      cell: (row) => (
        <div>
          <Link
            href={`/student/assignments/${row.id}`}
            className="font-medium text-slate-900 underline-offset-2 hover:underline dark:text-white"
          >
            {row.title}
          </Link>
          <p className="mt-0.5 text-xs text-slate-500">
            {row.subjectName} · {row.classCourseCode} · {row.teacherName}
          </p>
        </div>
      ),
    },
    {
      header: "Deadline",
      secondary: true,
      cell: (row) => (
        <div>
          <p>{formatDateTime(row.deadline)}</p>
          <p className={`text-xs ${isPast(row.deadline) ? "text-red-600" : "text-slate-500"}`}>
            {isPast(row.deadline) ? "closed " : ""}
            {relativeToNow(row.deadline)}
          </p>
        </div>
      ),
    },
    {
      header: "Status",
      cell: (row) =>
        row.hasSubmitted && row.submissionStatus ? (
          <SubmissionStatusBadge status={row.submissionStatus} />
        ) : isPast(row.deadline) && !row.allowLateSubmission ? (
          <Badge tone="red">Missed</Badge>
        ) : (
          <Badge tone="amber">Not submitted</Badge>
        ),
    },
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
  ];

  return (
    <>
      <PageHeader
        title="Assignments"
        description="Published work for the classes you are enrolled in."
      />

      <Card className="space-y-4">
        <div className="max-w-sm">
          <TextField
            label="Search"
            placeholder="Filter by title"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </div>

        <DataTable
          rows={data?.items}
          columns={columns}
          loading={loading}
          error={error}
          rowKey={(row) => row.id}
          empty="No assignments yet"
          emptyHint="Work appears here once a teacher publishes it to one of your classes."
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
