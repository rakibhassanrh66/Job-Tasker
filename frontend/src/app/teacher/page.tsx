// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import {
  ArrowRight,
  CalendarClock,
  ClipboardPlus,
  Clock,
  Files,
  Inbox,
  Plus,
} from "lucide-react";
import Link from "next/link";
import { Card, PageHeader, StatCard } from "@/components/ui";
import { api } from "@/lib/api";
import { formatDateTime, isPast } from "@/lib/format";
import { useApiQuery } from "@/lib/query";
import { AssignmentStatus, type AssignmentDto, type PagedResult } from "@/lib/types";

/**
 * Teacher dashboard — the Teaching Command Center. Live counts for the three actions a
 * teacher performs daily, then the pending workload: published assignments still
 * accepting submissions, newest first.
 */

export default function TeacherDashboard() {
  const { data: assignmentData } = useApiQuery(["teacher", "assignments"], () =>
    api.get<PagedResult<AssignmentDto>>("/assignments/mine?pageSize=5"),
  );

  const { data: pendingData } = useApiQuery(["teacher", "pending"], () =>
    api.get<PagedResult<AssignmentDto>>("/assignments/mine?pageSize=1&status=2"),
  );

const assignments = assignmentData?.items ?? [];
const pendingCount = pendingData?.totalCount ?? 0;
const publishedCount =
  assignments.filter((row) => row.status === AssignmentStatus.Published).length;
const draftCount = assignments.filter((row) => row.status === AssignmentStatus.Draft).length;

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Teacher"
        title="Teaching Command Center"
        description="Your workload at a glance — what is open, what is pending your review, and where to create next."
        action={
          <Link
            href="/teacher/assignments/new"
            className="inline-flex h-10 items-center gap-2 rounded-md bg-accent-400 px-4 text-sm font-semibold text-ink-950 transition-all duration-300 ease-expo hover:scale-[1.02] hover:bg-accent-300 hover:shadow-[0_0_24px_rgba(251,191,36,0.18)] active:scale-[0.99]"
          >
            <Plus className="h-4 w-4" aria-hidden />
            New Assignment
          </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="My Assignments"
          value={assignmentData?.totalCount ?? 0}
          tone="amber"
          icon={Files}
        />
        <StatCard title="Published" value={publishedCount} tone="emerald" />
        <StatCard title="Drafts" value={draftCount} tone="slate" />
        <StatCard
          title="Awaiting Review"
          value={pendingCount}
          description="assignments still open for submissions"
          tone="sky"
          icon={Inbox}
        />
      </div>

      <section className="mt-12">
        <div className="mb-5 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-xl font-extrabold tracking-tight text-white">Pending Workload</h2>
            <p className="mt-1 text-sm text-slate-400">
              Published assignments still accepting submissions.
            </p>
          </div>
          <Link
            href="/teacher/assignments"
            className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-accent-400 transition-colors duration-150 hover:text-accent-300"
          >
            All assignments
            <ArrowRight className="h-3.5 w-3.5" aria-hidden />
          </Link>
        </div>

        {assignmentData === undefined ? (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <Card key={i} className="p-5">
                <div className="skeleton h-4 w-1/3 rounded" />
                <div className="skeleton mt-3 h-3 w-1/2 rounded" />
              </Card>
            ))}
          </div>
        ) : assignments.filter((row) => row.status === AssignmentStatus.Published).length === 0 ? (
          <Card className="flex flex-col items-center justify-center p-12 text-center">
            <span className="mb-3 flex h-12 w-12 items-center justify-center rounded-md border border-line bg-ink-850 text-slate-500">
              <Clock className="h-6 w-6" aria-hidden />
            </span>
            <p className="text-base font-semibold text-slate-200">Nothing open right now</p>
            <p className="mt-1 max-w-sm text-sm text-slate-500">
              Publish a draft to start accepting submissions.
            </p>
            <Link
              href="/teacher/assignments/new"
              className="mt-5 inline-flex items-center gap-2 rounded-md border border-line-strong bg-ink-850 px-4 py-2 text-sm font-semibold text-slate-200 transition-colors duration-150 hover:border-ink-500 hover:text-white"
            >
              <ClipboardPlus className="h-4 w-4" aria-hidden />
              Create one
            </Link>
          </Card>
        ) : (
          <div className="space-y-3">
            {assignments
              .filter((row) => row.status === AssignmentStatus.Published)
              .map((assignment) => (
                <Link key={assignment.id} href={`/teacher/assignments/${assignment.id}`}>
                  <Card interactive className="p-5">
                    <div className="flex flex-wrap items-center justify-between gap-4">
                      <div className="min-w-0">
                        <h3 className="truncate font-bold text-white">{assignment.title}</h3>
                        <p className="mt-1 text-xs text-slate-500">
                          {assignment.subjectName} ·{" "}
                          <span className="font-mono">{assignment.classCourseCode}</span>
                        </p>
                      </div>

                      <div className="flex flex-wrap items-center gap-4">
                        <span
                          className={`inline-flex items-center gap-1.5 font-mono text-sm ${
                            isPast(assignment.deadline) ? "text-red-400" : "text-slate-300"
                          }`}
                        >
                          <CalendarClock className="h-4 w-4" aria-hidden />
                          {formatDateTime(assignment.deadline)}
                        </span>
                        <span className="inline-flex items-center gap-1.5 rounded-md border border-line bg-ink-850 px-2.5 py-1 font-mono text-xs text-slate-300">
                          <Inbox className="h-3.5 w-3.5 text-accent-400" aria-hidden />
                          {assignment.submissionCount} submission
                          {assignment.submissionCount === 1 ? "" : "s"}
                        </span>
                      </div>
                    </div>
                  </Card>
                </Link>
              ))}
          </div>
        )}
      </section>
    </div>
  );
}