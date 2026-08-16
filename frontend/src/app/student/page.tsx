// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import {
  ArrowRight,
  CalendarClock,
  CheckCircle2,
  Clock,
  FileText,
  GraduationCap,
  Inbox,
} from "lucide-react";
import Link from "next/link";
import { Card, PageHeader, StatCard } from "@/components/ui";
import { api } from "@/lib/api";
import { formatDateTime, isPast, relativeToNow } from "@/lib/format";
import { useApiQuery } from "@/lib/query";
import type { PagedResult, StudentAssignmentDto, SubmissionDto } from "@/lib/types";

/**
 * Student dashboard — My Academic Deployments. Counts across your available work, then
 * the priority queue: unsubmitted assignments with the closest deadlines first.
 */

export default function StudentDashboard() {
  const { data: assignmentData } = useApiQuery(["student", "available"], () =>
    api.get<PagedResult<StudentAssignmentDto>>("/assignments/available?pageSize=100"),
  );

  const { data: submissionData } = useApiQuery(["student", "submissions"], () =>
    api.get<PagedResult<SubmissionDto>>("/submissions/mine?pageSize=5"),
  );

  const assignments = assignmentData?.items ?? [];
  const submissions = submissionData?.items ?? [];

  const totalAssigned = assignmentData?.totalCount ?? 0;
  const pendingCount = assignments.filter((row) => !row.hasSubmitted && !isPast(row.deadline)).length;
  const submittedCount = assignments.filter((row) => row.hasSubmitted).length;
  const gradedCount = assignments.filter((row) => row.marks !== null).length;

  const queue = assignments
    .filter((row) => !row.hasSubmitted)
    .sort((a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime())
    .slice(0, 5);

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Student"
        title="My Academic Deployments"
        description="Everything your instructors have deployed to you — what is due, what is submitted, and what came back graded."
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Assigned Work" value={totalAssigned} tone="amber" icon={Inbox} />
        <StatCard title="Pending Submission" value={pendingCount} tone="sky" />
        <StatCard title="Submitted" value={submittedCount} tone="emerald" icon={CheckCircle2} />
        <StatCard title="Graded" value={gradedCount} tone="violet" icon={GraduationCap} />
      </div>

      {/* Priority queue */}
      <section className="mt-12">
        <div className="mb-5 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-xl font-extrabold tracking-tight text-white">Due Next</h2>
            <p className="mt-1 text-sm text-slate-400">
              Your open assignments, closest deadline first.
            </p>
          </div>
          <Link
            href="/student/assignments"
            className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-accent-400 transition-colors duration-150 hover:text-accent-300"
          >
            All assignments
            <ArrowRight className="h-3.5 w-3.5" aria-hidden />
          </Link>
        </div>

        {assignmentData === undefined ? (
          <div className="grid gap-4 sm:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <Card key={i} className="p-5">
                <div className="skeleton h-4 w-2/3 rounded" />
                <div className="skeleton mt-3 h-3 w-1/2 rounded" />
              </Card>
            ))}
          </div>
        ) : queue.length === 0 ? (
          <Card className="flex flex-col items-center justify-center p-12 text-center">
            <span className="mb-3 flex h-12 w-12 items-center justify-center rounded-md border border-line bg-ink-850 text-slate-500">
              <Clock className="h-6 w-6" aria-hidden />
            </span>
            <p className="text-base font-semibold text-slate-200">You are all caught up</p>
            <p className="mt-1 max-w-sm text-sm text-slate-500">
              No outstanding submissions. New published assignments will appear here.
            </p>
          </Card>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {queue.map((assignment) => {
              const expired = isPast(assignment.deadline);

              return (
                <Link key={assignment.id} href={`/student/assignments/${assignment.id}`}>
                  <Card interactive className="flex h-full flex-col p-5">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h3 className="truncate font-bold text-white">{assignment.title}</h3>
                        <p className="mt-1 text-xs text-slate-500">
                          {assignment.subjectName} ·{" "}
                          <span className="font-mono">{assignment.classCourseCode}</span>
                        </p>
                      </div>
                      <FileText className="h-4 w-4 shrink-0 text-slate-500" aria-hidden />
                    </div>

                    <div className="mt-4 flex items-center justify-between border-t border-line pt-4">
                      <span
                        className={`inline-flex items-center gap-1.5 font-mono text-xs ${
                          expired ? "text-red-400" : "text-slate-300"
                        }`}
                      >
                        <CalendarClock className="h-3.5 w-3.5" aria-hidden />
                        {formatDateTime(assignment.deadline)}
                      </span>
                      <span
                        className={`text-[11px] font-bold uppercase tracking-wider ${
                          expired ? "text-red-500" : "text-accent-400"
                        }`}
                      >
                        {expired ? "Closed " : "Due "}
                        {relativeToNow(assignment.deadline)}
                      </span>
                    </div>
                  </Card>
                </Link>
              );
            })}
          </div>
        )}
      </section>

      {/* Latest activity */}
      {submissions.length > 0 && (
        <section className="mt-12 pb-4">
          <h2 className="mb-5 text-xl font-extrabold tracking-tight text-white">
            Latest Submission Activity
          </h2>
          <div className="space-y-3">
            {submissions.map((submission) => (
              <Card key={submission.id} className="flex flex-wrap items-center justify-between gap-3 p-5">
                <div className="min-w-0">
                  <p className="truncate font-semibold text-white">{submission.assignmentTitle}</p>
                  <p className="mt-0.5 text-xs text-slate-500">
                    Handed in {formatDateTime(submission.submittedAt)}
                  </p>
                </div>
                <div className="flex items-center gap-4">
                  {submission.marks !== null ? (
                    <span className="font-mono text-sm font-bold text-emerald-300">
                      {submission.marks} / {submission.maxMarks}
                    </span>
                  ) : (
                    <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
                      Awaiting grade
                    </span>
                  )}
                  <Link
                    href={`/student/assignments/${submission.assignmentId}`}
                    className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-accent-400 transition-colors duration-150 hover:text-accent-300"
                  >
                    Open
                    <ArrowRight className="h-3 w-3" aria-hidden />
                  </Link>
                </div>
              </Card>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}