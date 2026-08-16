// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ArrowLeft, Eye, Inbox } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import {
  Badge,
  Button,
  Card,
  ErrorBanner,
  PageHeader,
  ProgressBar,
  SelectField,
  Spinner,
  StatCard,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { useApiMutation, useApiPagedQuery, useApiQuery } from "@/lib/query";
import {
  SubmissionStatus,
  submissionStatusLabels,
  type AssignmentDto,
  type PagedResult,
  type SubmissionDto,
} from "@/lib/types";

const PAGE_SIZE = 10;

/**
 * Legal lifecycle transitions, mirrored from SubmissionStatusPolicy.cs. A transition the
 * backend would reject is simply never offered — the dropdown only ever shows states the
 * API will accept for the submission's current status (F1).
 *
 *   Submitted | Late      → UnderReview | Graded | Returned
 *   UnderReview            → Graded | Returned
 *   Graded                 → Returned
 *   Returned               → (terminal)
 */
const LEGAL_TRANSITIONS: Record<SubmissionStatus, SubmissionStatus[]> = {
  [SubmissionStatus.Submitted]: [
    SubmissionStatus.UnderReview,
    SubmissionStatus.Graded,
    SubmissionStatus.Returned,
  ],
  [SubmissionStatus.Late]: [
    SubmissionStatus.UnderReview,
    SubmissionStatus.Graded,
    SubmissionStatus.Returned,
  ],
  [SubmissionStatus.UnderReview]: [SubmissionStatus.Graded, SubmissionStatus.Returned],
  [SubmissionStatus.Graded]: [SubmissionStatus.Returned],
  [SubmissionStatus.Returned]: [],
};

export default function AssignmentSubmissionsPage() {
  const { id } = useParams<{ id: string }>();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<SubmissionDto | null>(null);

  const assignment = useApiQuery<AssignmentDto>(
    ["assignments", id],
    () => api.get<AssignmentDto>(`/assignments/${id}`),
  );

  const submissions = useApiPagedQuery<SubmissionDto>(
    ["assignments", id, "submissions"],
    { page, pageSize: PAGE_SIZE, status: status || undefined },
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/assignments/${id}/submissions${query({ page, pageSize: PAGE_SIZE, status: status || undefined })}`,
      ),
  );

  const rows = submissions.data?.items ?? [];
  const totalCount = submissions.data?.totalCount ?? 0;
  const gradedCount = rows.filter(
    (row) => row.status === SubmissionStatus.Graded || row.marks !== null,
  ).length;
  const pendingCount = rows.filter((row) => row.marks === null).length;

  const columns: SortableColumn<SubmissionDto>[] = [
    {
      key: "studentName",
      header: "Student",
      sortValue: (row) => row.studentName.toLowerCase(),
      render: (row) => (
        <div className="flex items-center gap-3">
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-line bg-ink-850 text-[11px] font-bold text-slate-300">
            {row.studentName.slice(0, 2).toUpperCase()}
          </span>
          <div className="min-w-0">
            <p className="truncate font-semibold text-white">{row.studentName}</p>
            <p className="truncate font-mono text-xs text-slate-500">{row.studentEmail}</p>
          </div>
        </div>
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
          <span className="text-xs font-medium text-slate-500">Ungraded</span>
        ) : (
          <span className="font-bold text-accent-400">
            {row.marks} <span className="text-xs font-normal text-slate-500">/ {row.maxMarks}</span>
          </span>
        ),
    },
    {
      key: "action",
      header: "",
      className: "text-right",
      render: (row) => (
        <Button
          variant={row.marks === null ? "primary" : "secondary"}
          size="sm"
          onClick={() => setSelected(row)}
        >
          <Eye className="h-3.5 w-3.5" aria-hidden />
          {row.marks === null ? "Mark & Grade" : "Review Grade"}
        </Button>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <Link
        href={`/teacher/assignments/${id}`}
        className="mt-6 inline-flex items-center gap-1.5 text-xs font-bold uppercase tracking-widest text-slate-500 transition-colors duration-150 hover:text-accent-400"
      >
        <ArrowLeft className="h-3.5 w-3.5" aria-hidden />
        Back to assignment
      </Link>

      <PageHeader
        eyebrow="Teacher"
        title="Student Submissions & Grading"
        description={
          assignment.data
            ? `Reviewing submissions for: ${assignment.data.title}`
            : undefined
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Total Submissions" value={totalCount} tone="amber" icon={Inbox} />
        <StatCard title="Graded Work (this page)" value={gradedCount} tone="emerald" />
        <StatCard title="Pending Review (this page)" value={pendingCount} tone="sky" />
      </div>

      <div className="mt-8 grid gap-6 lg:grid-cols-3">
        <Card className="space-y-5 p-5 lg:col-span-2">
          <div className="w-full max-w-xs">
            <SelectField
              label="Filter by Status"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
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
            loading={submissions.data === undefined}
            emptyTitle="No submissions yet"
            emptyHint="Submissions appear here as students turn in their coursework."
            emptyIcon={Inbox}
          />

          <Pagination
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={totalCount}
            onPageChange={setPage}
          />
        </Card>

        <div>
          {selected ? (
            <GradePanel
              key={selected.id}
              submission={selected}
              onClose={() => setSelected(null)}
            />
          ) : (
            <Card className="flex flex-col items-center justify-center border-dashed p-8 text-center">
              <span className="mb-3 flex h-12 w-12 items-center justify-center rounded-md border border-line bg-ink-850 text-slate-500">
                <Eye className="h-6 w-6" aria-hidden />
              </span>
              <p className="text-sm font-semibold text-slate-200">No Submission Selected</p>
              <p className="mt-1 max-w-xs text-xs leading-relaxed text-slate-500">
                Select a submission from the list to review the answer text, inspect
                attachments, and enter marks.
              </p>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}

function GradePanel({
  submission,
  onClose,
}: {
  submission: SubmissionDto;
  onClose: () => void;
}) {
  const [marks, setMarks] = useState(submission.marks?.toString() ?? "");
  const [feedback, setFeedback] = useState(submission.feedback ?? "");
  const [nextStatus, setNextStatus] = useState("");
  const [failure, setFailure] = useState<unknown>(null);

  const alreadyGraded =
    submission.status === SubmissionStatus.Graded
    || submission.status === SubmissionStatus.Returned;

  const numeric = Number(marks);
  const marksValid =
    marks !== "" && Number.isInteger(numeric) && numeric >= 0 && numeric <= submission.maxMarks;

  const legalTransitions = LEGAL_TRANSITIONS[submission.status] ?? [];

  const saveGrade = useApiMutation<unknown, void>({
    mutationFn: () =>
      api.put(`/submissions/${submission.id}/grade`, {
        marks: numeric,
        feedback: feedback === "" ? null : feedback,
      }),
    invalidate: [
      ["assignments", submission.assignmentId, "submissions"],
      ["assignments", submission.assignmentId],
      ["admin", "submissions"],
    ],
    successMessage: `Graded ${submission.studentName}: ${numeric} / ${submission.maxMarks}`,
    onSuccess: () => onClose(),
  });

  const changeStatus = useApiMutation<unknown, void>({
    mutationFn: () =>
      api.put(`/submissions/${submission.id}/status`, { status: Number(nextStatus) }),
    invalidate: [
      ["assignments", submission.assignmentId, "submissions"],
      ["admin", "submissions"],
    ],
    successMessage: () => `Status updated to ${submissionStatusLabels[Number(nextStatus) as SubmissionStatus]}.`,
    onSuccess: () => {
      setNextStatus("");
      onClose();
    },
  });

  const busy = saveGrade.isPending || changeStatus.isPending;

  return (
    <Card className="space-y-5 p-5">
      <div className="flex items-start justify-between gap-3 border-b border-line pb-4">
        <div>
          <h2 className="text-base font-bold text-white">{submission.studentName}</h2>
          <p className="mt-0.5 text-xs text-slate-500">{formatDateTime(submission.submittedAt)}</p>
        </div>
        <Button variant="ghost" size="sm" onClick={onClose}>
          Close
        </Button>
      </div>

      <div className="flex items-center justify-between">
        <SubmissionStatusBadge status={submission.status} />
        {submission.marks !== null && (
          <Badge tone="green">
            Grade: {Math.round((submission.marks / submission.maxMarks) * 100)}%
          </Badge>
        )}
      </div>

      <ErrorBanner error={failure} />

      {/* Answer preview */}
      <div>
        <p className="mb-1.5 text-[11px] font-bold uppercase tracking-widest text-slate-500">
          Student Answer
        </p>
        <div className="max-h-56 overflow-y-auto rounded-md border border-line bg-ink-950/60 p-3.5">
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-slate-200">
            {submission.answerText}
          </p>
        </div>
      </div>

      {submission.attachmentUrl && (
        <div className="rounded-md border border-line bg-ink-850/60 p-3.5">
          <p className="text-[11px] font-bold uppercase tracking-widest text-slate-500">
            Attached Resource
          </p>
          <a
            href={submission.attachmentUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="mt-1 block truncate font-mono text-xs text-accent-400 transition-colors duration-150 hover:text-accent-300 hover:underline"
          >
            {submission.attachmentUrl}
          </a>
        </div>
      )}

      <hr className="border-line" />

      {alreadyGraded ? (
        <div className="space-y-3 rounded-md border border-emerald-500/40 bg-emerald-950/30 p-4">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold uppercase tracking-widest text-emerald-300">
              Evaluated Score
            </span>
            <span className="text-lg font-bold text-emerald-300">
              {submission.marks} / {submission.maxMarks}
            </span>
          </div>

          <ProgressBar value={submission.marks ?? 0} max={submission.maxMarks} tone="emerald" />

          {submission.feedback && (
            <div className="mt-2 text-sm">
              <span className="font-semibold text-slate-300">Teacher Feedback:</span>
              <p className="mt-1 whitespace-pre-wrap text-slate-400">{submission.feedback}</p>
            </div>
          )}

          {submission.gradedByTeacherName && (
            <p className="pt-1 text-[11px] text-slate-500">
              Marked by {submission.gradedByTeacherName}
              {submission.gradedAt ? ` on ${formatDateTime(submission.gradedAt)}` : ""}
            </p>
          )}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="space-y-1.5">
            <TextField
              label={`Assign Marks (0 – ${submission.maxMarks})`}
              type="number"
              min={0}
              max={submission.maxMarks}
              value={marks}
              disabled={busy}
              onChange={(event) => setMarks(event.target.value)}
              error={
                marks !== "" && !marksValid
                  ? `Enter an integer between 0 and ${submission.maxMarks}.`
                  : undefined
              }
            />
            {marksValid && (
              <div className="pt-1">
                <ProgressBar
                  value={numeric}
                  max={submission.maxMarks}
                  label="Calculated Score"
                  tone="amber"
                />
              </div>
            )}
          </div>

          <TextAreaField
            label="Feedback & Comments (Optional)"
            placeholder="Write constructive evaluation comments for the student…"
            rows={3}
            value={feedback}
            disabled={busy}
            onChange={(event) => setFeedback(event.target.value)}
          />

          <Button
            className="w-full"
            disabled={busy || !marksValid}
            onClick={() => {
              setFailure(null);
              void saveGrade.mutateAsync().catch((cause) => setFailure(cause));
            }}
          >
            {saveGrade.isPending ? "Saving…" : "Submit Grade & Feedback"}
          </Button>
        </div>
      )}

      <hr className="border-line" />

      {/* Lifecycle transitions — only states the policy accepts (F1) */}
      <div className="space-y-2">
        {legalTransitions.length === 0 ? (
          <p className="rounded-md border border-line bg-ink-850/60 px-3 py-2.5 text-xs text-slate-500">
            Returned submissions are terminal — no further transitions are allowed by the
            submission policy.
          </p>
        ) : (
          <>
            <SelectField
              label="Change Lifecycle Status"
              hint="Only legal transitions are offered — the API rejects everything else."
              value={nextStatus}
              disabled={busy}
              onChange={(event) => setNextStatus(event.target.value)}
            >
              <option value="">Choose Transition…</option>
              {legalTransitions.map((value) => (
                <option key={value} value={value}>
                  {submissionStatusLabels[value]}
                </option>
              ))}
            </SelectField>

            <Button
              variant="secondary"
              className="w-full"
              disabled={busy || !nextStatus}
              onClick={() => {
                setFailure(null);
                void changeStatus.mutateAsync().catch((cause) => setFailure(cause));
              }}
            >
              {changeStatus.isPending ? "Updating…" : "Update Submission Status"}
            </Button>
          </>
        )}
      </div>

      {busy && <Spinner label="Working…" />}
    </Card>
  );
}