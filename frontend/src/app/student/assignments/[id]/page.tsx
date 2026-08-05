// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { SubmissionStatusBadge } from "@/components/status-badge";
import {
  Badge,
  Button,
  Card,
  ErrorBanner,
  PageHeader,
  Spinner,
  SuccessBanner,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { ApiError, api } from "@/lib/api";
import { formatDateTime, isPast, relativeToNow } from "@/lib/format";
import { SubmissionStatus, type StudentAssignmentDto, type SubmissionDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

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

  const assignment = useApi<StudentAssignmentDto>(
    () => api.get<StudentAssignmentDto>(`/assignments/${id}`),
    [id],
  );

  // Only fetched once the assignment says there is one, so a student with no submission
  // does not generate a guaranteed 404.
  const submissionId = assignment.data?.submissionId ?? null;

  const submission = useApi<SubmissionDto | null>(
    () => (submissionId ? api.get<SubmissionDto>(`/submissions/${submissionId}`) : Promise.resolve(null)),
    [submissionId],
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

  // Populate the form from an existing submission once it arrives.
  useEffect(() => {
    if (submission.data) {
      reset({
        answerText: submission.data.answerText,
        attachmentUrl: submission.data.attachmentUrl ?? "",
      });
    }
  }, [submission.data, reset]);

  if (assignment.loading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (assignment.error || !assignment.data) {
    return <ErrorBanner error={assignment.error ?? "Assignment not found."} />;
  }

  const item = assignment.data;
  const existing = submission.data;
  const closed = isPast(item.deadline);
  const graded =
    existing !== null &&
    existing !== undefined &&
    existing.status !== SubmissionStatus.Submitted &&
    existing.status !== SubmissionStatus.Late;

  /**
   * Why the form is unavailable, if it is. Mirrors the server's rules so the reason is
   * visible before a request rather than arriving as a 409 — the server still decides.
   */
  const blocked = !existing
    ? closed && !item.allowLateSubmission
      ? "The deadline has passed and this assignment does not accept late work."
      : null
    : graded
      ? "This submission has been reviewed, so it can no longer be changed."
      : !item.allowUpdateBeforeDeadline
        ? "This assignment does not allow submissions to be updated."
        : closed
          ? "The deadline has passed, so this submission can no longer be updated."
          : null;

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);
    setSaved(null);

    const body = {
      answerText: values.answerText,
      attachmentUrl: values.attachmentUrl === "" ? null : values.attachmentUrl,
    };

    try {
      if (existing) {
        await api.put<SubmissionDto>(`/submissions/${existing.id}`, body);
        setSaved("Your submission has been updated.");
      } else {
        await api.post<SubmissionDto>(`/assignments/${item.id}/submit`, body);
        setSaved("Your answer has been submitted.");
      }

      assignment.reload();
      submission.reload();
    } catch (cause) {
      // A 422 names the field it rejected, so put the message back on that field.
      if (cause instanceof ApiError && cause.status === 422) {
        for (const [field, message] of Object.entries(cause.fieldErrors)) {
          if (field === "answerText" || field === "attachmentUrl") {
            setError(field, { message });
          }
        }
      }

      setFailure(cause);
    }
  });

  return (
    <>
      <PageHeader
        title={item.title}
        description={`${item.subjectName} · ${item.classCourseCode} · set by ${item.teacherName}`}
        action={
          <Link href="/student/assignments">
            <Button variant="secondary">Back to list</Button>
          </Link>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="space-y-4 lg:col-span-2">
          <div>
            <h2 className="text-sm font-medium text-slate-700 dark:text-slate-200">Brief</h2>
            <p className="mt-1.5 text-sm whitespace-pre-wrap text-slate-700 dark:text-slate-300">
              {item.description}
            </p>
          </div>

          <hr className="border-slate-200 dark:border-slate-700" />

          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <h2 className="text-sm font-medium text-slate-700 dark:text-slate-200">
              {existing ? "Your submission" : "Submit your answer"}
            </h2>

            {saved && <SuccessBanner>{saved}</SuccessBanner>}
            <ErrorBanner error={failure} />

            {blocked && (
              <p className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
                {blocked}
              </p>
            )}

            <TextAreaField
              label="Answer"
              rows={10}
              disabled={blocked !== null}
              error={errors.answerText?.message}
              {...register("answerText")}
            />

            <TextField
              label="Attachment URL"
              placeholder="https://…"
              hint="Optional. A link to your work, if it lives elsewhere."
              disabled={blocked !== null}
              error={errors.attachmentUrl?.message}
              {...register("attachmentUrl")}
            />

            {!blocked && (
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Saving…" : existing ? "Update submission" : "Submit"}
              </Button>
            )}
          </form>
        </Card>

        <div className="space-y-6">
          <Card className="space-y-3 text-sm">
            <h2 className="text-sm font-medium text-slate-700 dark:text-slate-200">Details</h2>

            <div>
              <p className="text-xs text-slate-500">Deadline</p>
              <p>{formatDateTime(item.deadline)}</p>
              <p className={`text-xs ${closed ? "text-red-600" : "text-slate-500"}`}>
                {closed ? "closed " : ""}
                {relativeToNow(item.deadline)}
              </p>
            </div>

            <div>
              <p className="text-xs text-slate-500">Maximum marks</p>
              <p>{item.maxMarks}</p>
            </div>

            <div className="flex flex-wrap gap-2 pt-1">
              <Badge tone={item.allowLateSubmission ? "green" : "neutral"}>
                {item.allowLateSubmission ? "Late work accepted" : "No late work"}
              </Badge>
              <Badge tone={item.allowUpdateBeforeDeadline ? "green" : "neutral"}>
                {item.allowUpdateBeforeDeadline ? "Updates allowed" : "No updates"}
              </Badge>
            </div>
          </Card>

          {existing && (
            <Card className="space-y-3 text-sm">
              <h2 className="text-sm font-medium text-slate-700 dark:text-slate-200">Result</h2>

              <div className="flex items-center gap-2">
                <SubmissionStatusBadge status={existing.status} />
              </div>

              <div>
                <p className="text-xs text-slate-500">Submitted</p>
                <p>{formatDateTime(existing.submittedAt)}</p>
              </div>

              <div>
                <p className="text-xs text-slate-500">Marks</p>
                <p className="text-lg font-semibold">
                  {existing.marks === null ? "Not marked yet" : `${existing.marks} / ${existing.maxMarks}`}
                </p>
              </div>

              {existing.feedback && (
                <div>
                  <p className="text-xs text-slate-500">Feedback</p>
                  <p className="whitespace-pre-wrap">{existing.feedback}</p>
                </div>
              )}
            </Card>
          )}
        </div>
      </div>
    </>
  );
}
