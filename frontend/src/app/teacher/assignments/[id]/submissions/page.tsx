// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { SubmissionStatusBadge } from "@/components/status-badge";
import {
  Button,
  Card,
  ErrorBanner,
  PageHeader,
  SelectField,
  SuccessBanner,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import {
  SubmissionStatus,
  submissionStatusLabels,
  type AssignmentDto,
  type PagedResult,
  type SubmissionDto,
} from "@/lib/types";
import { useApi } from "@/lib/use-api";

/**
 * The grading queue for one assignment.
 *
 * Marking happens in a panel beside the list rather than on its own page, because a marker
 * works through a queue and a round trip per submission would be the slowest part of the
 * job. The status control is separate from grading on purpose: entering marks *is* the
 * review, so grading moves a submission to Graded by itself, while the status dropdown
 * covers the transitions that are not grading — putting something under review, or
 * returning it.
 */
export default function AssignmentSubmissionsPage() {
  const { id } = useParams<{ id: string }>();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<SubmissionDto | null>(null);

  const assignment = useApi<AssignmentDto>(() => api.get<AssignmentDto>(`/assignments/${id}`), [id]);

  const submissions = useApi<PagedResult<SubmissionDto>>(
    () =>
      api.get<PagedResult<SubmissionDto>>(
        `/assignments/${id}/submissions${query({ page, pageSize: 10, status: status || undefined })}`,
      ),
    [id, page, status],
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
      header: "",
      align: "right",
      cell: (row) => (
        <Button variant="secondary" onClick={() => setSelected(row)}>
          {row.marks === null ? "Mark" : "Review"}
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Submissions"
        description={assignment.data ? assignment.data.title : undefined}
        action={
          <Link href={`/teacher/assignments/${id}`}>
            <Button variant="secondary">Back to assignment</Button>
          </Link>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="space-y-4 lg:col-span-2">
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
            rows={submissions.data?.items}
            columns={columns}
            loading={submissions.loading}
            error={submissions.error}
            rowKey={(row) => row.id}
            empty="Nothing submitted yet"
            emptyHint="Submissions appear here as students hand work in."
          />

          {submissions.data && <Pagination page={submissions.data} onPageChange={setPage} />}
        </Card>

        <div>
          {selected ? (
            <GradePanel
              submission={selected}
              onClose={() => setSelected(null)}
              onSaved={() => {
                setSelected(null);
                submissions.reload();
                assignment.reload();
              }}
            />
          ) : (
            <Card>
              <p className="text-sm text-slate-500">
                Choose a submission to read the answer and enter marks.
              </p>
            </Card>
          )}
        </div>
      </div>
    </>
  );
}

function GradePanel({
  submission,
  onClose,
  onSaved,
}: {
  submission: SubmissionDto;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [marks, setMarks] = useState(submission.marks?.toString() ?? "");
  const [feedback, setFeedback] = useState(submission.feedback ?? "");
  const [nextStatus, setNextStatus] = useState("");
  const [failure, setFailure] = useState<unknown>(null);
  const [saved, setSaved] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // The API refuses to grade something already graded — one mark, entered once — so the
  // form says so rather than offering a button that can only 409.
  const alreadyGraded =
    submission.status === SubmissionStatus.Graded || submission.status === SubmissionStatus.Returned;

  const numeric = Number(marks);
  const marksValid =
    marks !== "" && Number.isInteger(numeric) && numeric >= 0 && numeric <= submission.maxMarks;

  const grade = async () => {
    setBusy(true);
    setFailure(null);
    setSaved(null);

    try {
      await api.put(`/submissions/${submission.id}/grade`, {
        marks: numeric,
        feedback: feedback === "" ? null : feedback,
      });

      setSaved("Marks saved.");
      onSaved();
    } catch (cause) {
      setFailure(cause);
    } finally {
      setBusy(false);
    }
  };

  const changeStatus = async () => {
    setBusy(true);
    setFailure(null);
    setSaved(null);

    try {
      await api.put(`/submissions/${submission.id}/status`, { status: Number(nextStatus) });

      setSaved("Status updated.");
      onSaved();
    } catch (cause) {
      setFailure(cause);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Card className="space-y-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-medium text-slate-900 dark:text-white">
            {submission.studentName}
          </h2>
          <p className="text-xs text-slate-500">{formatDateTime(submission.submittedAt)}</p>
        </div>
        <Button variant="ghost" onClick={onClose}>
          Close
        </Button>
      </div>

      <SubmissionStatusBadge status={submission.status} />

      {saved && <SuccessBanner>{saved}</SuccessBanner>}
      <ErrorBanner error={failure} />

      <div>
        <p className="text-xs text-slate-500">Answer</p>
        <p className="mt-1 max-h-64 overflow-y-auto text-sm whitespace-pre-wrap text-slate-800 dark:text-slate-200">
          {submission.answerText}
        </p>
      </div>

      {submission.attachmentUrl && (
        <a
          href={submission.attachmentUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="block text-sm text-blue-700 underline underline-offset-2 dark:text-blue-400"
        >
          Open attachment
        </a>
      )}

      <hr className="border-slate-200 dark:border-slate-700" />

      {alreadyGraded ? (
        <div className="space-y-2 text-sm">
          <p className="font-medium text-slate-700 dark:text-slate-200">
            Marked {submission.marks} / {submission.maxMarks}
          </p>
          {submission.feedback && (
            <p className="whitespace-pre-wrap text-slate-600 dark:text-slate-400">
              {submission.feedback}
            </p>
          )}
          {submission.gradedByTeacherName && (
            <p className="text-xs text-slate-500">
              by {submission.gradedByTeacherName}
              {submission.gradedAt ? ` · ${formatDateTime(submission.gradedAt)}` : ""}
            </p>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          <TextField
            label={`Marks (0–${submission.maxMarks})`}
            type="number"
            min={0}
            max={submission.maxMarks}
            value={marks}
            onChange={(event) => setMarks(event.target.value)}
            error={
              marks !== "" && !marksValid
                ? `Enter a whole number between 0 and ${submission.maxMarks}.`
                : undefined
            }
          />

          <TextAreaField
            label="Feedback"
            rows={4}
            value={feedback}
            onChange={(event) => setFeedback(event.target.value)}
          />

          <Button disabled={busy || !marksValid} onClick={() => void grade()}>
            {busy ? "Saving…" : "Save marks"}
          </Button>
        </div>
      )}

      <hr className="border-slate-200 dark:border-slate-700" />

      <div className="space-y-3">
        <SelectField
          label="Change status"
          hint="For transitions other than grading."
          value={nextStatus}
          onChange={(event) => setNextStatus(event.target.value)}
        >
          <option value="">Choose…</option>
          {Object.values(SubmissionStatus)
            // Late is only ever set at submission time, never transitioned into.
            .filter((value) => value !== SubmissionStatus.Late && value !== submission.status)
            .map((value) => (
              <option key={value} value={value}>
                {submissionStatusLabels[value]}
              </option>
            ))}
        </SelectField>

        <Button variant="secondary" disabled={busy || !nextStatus} onClick={() => void changeStatus()}>
          Update status
        </Button>
      </div>
    </Card>
  );
}
