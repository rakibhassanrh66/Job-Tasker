// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, CalendarClock, FileText, Send } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { SubmissionStatusBadge } from "@/components/status-badge";
import {
  Button,
  Card,
  ErrorBanner,
  PageHeader,
  ProgressBar,
  Spinner,
  SuccessBanner,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { ApiError, api } from "@/lib/api";
import { formatDateTime, isPast, relativeToNow } from "@/lib/format";
import { useApiMutation, useApiQuery } from "@/lib/query";
import { SubmissionStatus, type StudentAssignmentDto, type SubmissionDto } from "@/lib/types";

const schema = z.object({
  answerText: z.string().min(1, "An answer is required.").max(20000),
  attachmentUrl: z
    .string()
    .max(2000)
    .refine((value) => value === "" || /^https?:\/\//i.test(value), {
      message: "Enter a full URL starting with http:// or https://",
    }),
});

type FormValues = z.infer<typeof schema>;

export default function StudentAssignmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [saved, setSaved] = useState<string | null>(null);
  const [failure, setFailure] = useState<unknown>(null);

  const assignment = useApiQuery<StudentAssignmentDto>(
    ["assignments", id, "student"],
    () => api.get<StudentAssignmentDto>(`/assignments/${id}`),
  );

  const submissionId = assignment.data?.submissionId ?? null;

  const submission = useApiQuery<SubmissionDto | null>(
    ["submissions", submissionId ?? "none"],
    () =>
      submissionId
        ? api.get<SubmissionDto>(`/submissions/${submissionId}`)
        : Promise.resolve(null),
    submissionId !== null,
  );

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { answerText: "", attachmentUrl: "" },
  });

  useEffect(() => {
    if (submission.data) {
      reset({
        answerText: submission.data.answerText,
        attachmentUrl: submission.data.attachmentUrl ?? "",
      });
    }
  }, [submission.data, reset]);

  // Hooks stay above the early returns so their order is stable across renders.
  const saveSubmission = useApiMutation<SubmissionDto, FormValues>({
    mutationFn: (values) => {
      const body = {
        answerText: values.answerText,
        attachmentUrl: values.attachmentUrl === "" ? null : values.attachmentUrl,
      };

      return existing
        ? api.put<SubmissionDto>(`/submissions/${existing.id}`, body)
        : api.post<SubmissionDto>(`/assignments/${item.id}/submit`, body);
    },
    invalidate: [["assignments", id, "student"], ["student", "available"], ["student", "submissions"]],
    onError: (cause) => {
      if (cause instanceof ApiError && cause.status === 422) {
        for (const [field, message] of Object.entries(cause.fieldErrors)) {
          if (field === "answerText" || field === "attachmentUrl") {
            setError(field, { message });
          }
        }
      }
    },
  });

  if (assignment.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner label="Loading assignment details" />
      </div>
    );
  }

  if (assignment.error || !assignment.data) {
    return (
      <div className="mx-auto w-full max-w-7xl px-4 py-12">
        <ErrorBanner error={assignment.error ?? "Assignment not found."} />
      </div>
    );
  }

  const item = assignment.data;
  const existing = submission.data;
  const closed = isPast(item.deadline);

  const graded =
    existing !== null
    && existing !== undefined
    && existing.status !== SubmissionStatus.Submitted
    && existing.status !== SubmissionStatus.Late;

  const blocked = !existing
    ? closed && !item.allowLateSubmission
      ? "The deadline has passed and this assignment does not accept late work."
      : null
    : graded
      ? "This submission has been reviewed and graded, so it can no longer be changed."
      : !item.allowUpdateBeforeDeadline
        ? "This assignment does not allow submissions to be updated once turned in."
        : closed
          ? "The deadline has passed, so this submission can no longer be updated."
          : null;

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);
    setSaved(null);

    try {
      await saveSubmission.mutateAsync(values);
      const message = existing
        ? "Your submission has been updated successfully."
        : "Your answer has been submitted successfully.";
      setSaved(message);
    } catch (cause) {
      setFailure(cause);
    }
  });

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <Link
        href="/student/assignments"
        className="mt-6 inline-flex items-center gap-1.5 text-xs font-bold uppercase tracking-widest text-slate-500 transition-colors duration-150 hover:text-accent-400"
      >
        <ArrowLeft className="h-3.5 w-3.5" aria-hidden />
        Available assignments
      </Link>

      <PageHeader
        eyebrow="Student"
        title={item.title}
        description={`${item.subjectName} · Course: ${item.classCourseCode} · Instructor: ${item.teacherName}`}
      />

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Brief & submission */}
        <Card className="space-y-5 p-6 lg:col-span-2">
          <div>
            <p className="mb-2 text-[11px] font-bold uppercase tracking-widest text-slate-500">
              Assignment Instructions & Brief
            </p>
            <div className="rounded-md border border-line bg-ink-950/60 p-4">
              <p className="whitespace-pre-wrap text-sm leading-relaxed text-slate-200">
                {item.description}
              </p>
            </div>
          </div>

          <hr className="border-line" />

          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-white">
                {existing ? "Your Submitted Work" : "Turn in Your Submission"}
              </h2>
              {existing && <SubmissionStatusBadge status={existing.status} />}
            </div>

            {saved && <SuccessBanner>{saved}</SuccessBanner>}
            <ErrorBanner error={failure} />

            {blocked && (
              <div className="flex gap-3 rounded-md border border-accent-400/40 bg-amber-950/40 p-4 text-sm text-amber-200">
                <CalendarClock className="mt-0.5 h-4 w-4 shrink-0 text-accent-400" aria-hidden />
                <span>{blocked}</span>
              </div>
            )}

            <div className="space-y-1">
              <TextAreaField
                label="Your Answer"
                rows={10}
                placeholder="Type or paste your complete solution here…"
                disabled={blocked !== null}
                error={errors.answerText?.message}
                {...register("answerText")}
              />
            </div>

            <TextField
              label="Project Repository or Attachment URL (Optional)"
              placeholder="https://github.com/… or https://drive.google.com/…"
              hint="Include a live demo link, GitHub repository, or cloud document link if requested."
              disabled={blocked !== null}
              error={errors.attachmentUrl?.message}
              {...register("attachmentUrl")}
            />

            {!blocked && (
              <div className="pt-2">
                <Button
                  type="submit"
                  className="px-6"
                  disabled={isSubmitting || saveSubmission.isPending}
                >
                  {isSubmitting || saveSubmission.isPending ? (
                    "Submitting…"
                  ) : existing ? (
                    <>
                      <Send className="h-4 w-4" aria-hidden />
                      Save Submission Update
                    </>
                  ) : (
                    "Submit Assignment Answer"
                  )}
                </Button>
              </div>
            )}
          </form>
        </Card>

        {/* Rules & result */}
        <div className="space-y-5">
          <Card className="space-y-4 p-5">
            <p className="border-b border-line pb-2 text-[11px] font-bold uppercase tracking-widest text-slate-500">
              Assignment Rules & Schedule
            </p>

            <div className="space-y-3">
              <div>
                <p className="text-xs text-slate-500">Submission Deadline</p>
                <p className="mt-0.5 font-semibold text-white">{formatDateTime(item.deadline)}</p>
                <p
                  className={`mt-0.5 text-xs ${
                    closed ? "font-bold text-red-400" : "font-medium text-emerald-400"
                  }`}
                >
                  {closed ? "Closed " : "Due "}
                  {relativeToNow(item.deadline)}
                </p>
              </div>

              <div>
                <p className="text-xs text-slate-500">Maximum Marks</p>
                <p className="mt-0.5 text-xl font-bold text-accent-400">{item.maxMarks} pts</p>
              </div>

              <div className="flex flex-col gap-1.5 border-t border-line pt-2">
                <span className="flex items-center gap-2 text-xs text-slate-300">
                  <span
                    className={`h-2 w-2 rounded-full ${
                      item.allowLateSubmission ? "bg-emerald-500" : "bg-slate-500"
                    }`}
                    aria-hidden
                  />
                  {item.allowLateSubmission
                    ? "Late submissions accepted"
                    : "Strict deadline (no late work)"}
                </span>
                <span className="flex items-center gap-2 text-xs text-slate-300">
                  <span
                    className={`h-2 w-2 rounded-full ${
                      item.allowUpdateBeforeDeadline ? "bg-emerald-500" : "bg-slate-500"
                    }`}
                    aria-hidden
                  />
                  {item.allowUpdateBeforeDeadline
                    ? "Revisions allowed before deadline"
                    : "Single submission only"}
                </span>
              </div>
            </div>
          </Card>

          {existing && (
            <Card className="space-y-4 p-5">
              <p className="border-b border-line pb-2 text-[11px] font-bold uppercase tracking-widest text-accent-400">
                Submission Evaluation
              </p>

              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-500">Status:</span>
                <SubmissionStatusBadge status={existing.status} />
              </div>

              <div className="space-y-1.5">
                <p className="text-xs text-slate-500">Score Received</p>
                {existing.marks === null ? (
                  <p className="text-sm font-medium text-accent-400">
                    Pending Evaluation by Instructor
                  </p>
                ) : (
                  <div className="space-y-2">
                    <p className="text-2xl font-bold text-emerald-300">
                      {existing.marks}{" "}
                      <span className="text-sm font-normal text-slate-500">
                        / {existing.maxMarks}
                      </span>
                    </p>
                    <ProgressBar value={existing.marks} max={existing.maxMarks} tone="emerald" />
                  </div>
                )}
              </div>

              {existing.feedback && (
                <div className="border-t border-line pt-2">
                  <p className="text-xs font-bold uppercase tracking-widest text-slate-500">
                    Teacher Feedback
                  </p>
                  <blockquote className="mt-1.5 whitespace-pre-wrap rounded-md border-l-2 border-accent-400 bg-ink-850/60 p-3 text-sm italic text-slate-300">
                    {existing.feedback}
                  </blockquote>
                </div>
              )}

              <p className="flex items-center gap-1.5 pt-1 text-[11px] text-slate-500">
                <FileText className="h-3 w-3" aria-hidden />
                Handed in: {formatDateTime(existing.submittedAt)}
              </p>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}