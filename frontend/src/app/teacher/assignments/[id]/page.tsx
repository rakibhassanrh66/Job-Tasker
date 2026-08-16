// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Inbox, Send, Trash2 } from "lucide-react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { ConfirmDialog } from "@/components/modal";
import { AssignmentStatusBadge } from "@/components/status-badge";
import {
  Button,
  Card,
  CheckboxField,
  ErrorBanner,
  PageHeader,
  Spinner,
  SuccessBanner,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { ApiError, api } from "@/lib/api";
import { formatDateTime, fromLocalInputValue, toLocalInputValue } from "@/lib/format";
import { useApiMutation, useApiQuery } from "@/lib/query";
import { AssignmentStatus, type AssignmentDto } from "@/lib/types";

// No subject or class here: UpdateAssignmentRequest deliberately omits them. Moving work
// to a different class after students have submitted would orphan their submissions.
const schema = z.object({
  title: z.string().min(1, "Title is required.").max(300),
  description: z.string().min(1, "Description is required.").max(5000),
  deadline: z.string().min(1, "A deadline is required."),
  maxMarks: z.coerce.number().int().min(1, "Maximum marks must be at least 1.").max(1000),
  allowLateSubmission: z.boolean(),
  allowUpdateBeforeDeadline: z.boolean(),
});

type FormValues = z.input<typeof schema>;

export default function EditAssignmentPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [saved, setSaved] = useState(false);
  const [failure, setFailure] = useState<unknown>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const assignment = useApiQuery<AssignmentDto>(
    ["assignments", id],
    () => api.get<AssignmentDto>(`/assignments/${id}`),
  );

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (assignment.data) {
      reset({
        title: assignment.data.title,
        description: assignment.data.description,
        deadline: toLocalInputValue(assignment.data.deadline),
        maxMarks: assignment.data.maxMarks,
        allowLateSubmission: assignment.data.allowLateSubmission,
        allowUpdateBeforeDeadline: assignment.data.allowUpdateBeforeDeadline,
      });
    }
  }, [assignment.data, reset]);

  // Hooks stay above the early returns so their order is stable across renders.
  const save = useApiMutation<AssignmentDto, FormValues>({
    mutationFn: (values) =>
      api.put<AssignmentDto>(`/assignments/${id}`, {
        title: values.title,
        description: values.description,
        deadline: fromLocalInputValue(values.deadline),
        maxMarks: values.maxMarks,
        allowLateSubmission: values.allowLateSubmission,
        allowUpdateBeforeDeadline: values.allowUpdateBeforeDeadline,
      }),
    invalidate: [["assignments", id], ["teacher", "assignments"], ["admin", "assignments"]],
    onError: (cause) => {
      if (cause instanceof ApiError && cause.status === 422) {
        for (const [field, message] of Object.entries(cause.fieldErrors)) {
          if (field in schema.shape) {
            setError(field as keyof FormValues, { message });
          }
        }
      }
    },
  });

  const publish = useApiMutation<unknown, void>({
    mutationFn: () => api.post(`/assignments/${id}/publish`),
    invalidate: [["assignments", id], ["teacher", "assignments"], ["teacher", "pending"]],
    successMessage: "Assignment published.",
  });

  const remove = useApiMutation<unknown, void>({
    mutationFn: () => api.del(`/assignments/${id}`),
    onSuccess: () => router.push("/teacher/assignments"),
  });

  if (assignment.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner label="Loading assignment" />
      </div>
    );
  }

  if (assignment.error || !assignment.data) {
    return (
      <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 py-12">
        <ErrorBanner error={assignment.error ?? "Assignment not found."} />
      </div>
    );
  }

  const item = assignment.data;

  const onSubmit = handleSubmit(async (raw) => {
    const values = schema.parse(raw);
    setFailure(null);
    setSaved(false);

    try {
      await save.mutateAsync(values);
      setSaved(true);
    } catch (cause) {
      setFailure(cause);
    }
  });

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <Link
        href="/teacher/assignments"
        className="mt-6 inline-flex items-center gap-1.5 text-xs font-bold uppercase tracking-widest text-slate-500 transition-colors duration-150 hover:text-accent-400"
      >
        <ArrowLeft className="h-3.5 w-3.5" aria-hidden />
        My assignments
      </Link>

      <PageHeader
        eyebrow="Teacher"
        title={item.title}
        description={`${item.subjectName} · ${item.classCourseCode}`}
        action={
          <div className="flex flex-wrap gap-2">
            <Link href={`/teacher/assignments/${id}/submissions`}>
              <Button variant="secondary">
                <Inbox className="h-4 w-4" aria-hidden />
                Submissions ({item.submissionCount})
              </Button>
            </Link>
            {item.status === AssignmentStatus.Draft && (
              <Button
                disabled={publish.isPending}
                onClick={() => void publish.mutateAsync()}
              >
                <Send className="h-4 w-4" aria-hidden />
                {publish.isPending ? "Publishing…" : "Publish"}
              </Button>
            )}
            <Button variant="ghost" disabled={remove.isPending} onClick={() => setDeleteOpen(true)}>
              <Trash2 className="h-4 w-4 text-red-400" aria-hidden />
              Delete
            </Button>
          </div>
        }
      />

      <div className="flex flex-wrap items-center gap-3 pb-6 text-sm text-slate-500">
        <AssignmentStatusBadge status={item.status} />
        <span>Created {formatDateTime(item.createdAt)}</span>
        <span>·</span>
        <span>Updated {formatDateTime(item.updatedAt)}</span>
      </div>

      <Card className="max-w-2xl p-6">
        <form onSubmit={onSubmit} className="space-y-5" noValidate>
          {saved && <SuccessBanner>Changes saved.</SuccessBanner>}
          <ErrorBanner error={failure} />

          <TextField label="Title" error={errors.title?.message} {...register("title")} />

          <TextAreaField
            label="Description"
            rows={6}
            error={errors.description?.message}
            {...register("description")}
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <TextField
              label="Deadline"
              type="datetime-local"
              error={errors.deadline?.message}
              {...register("deadline")}
            />

            <TextField
              label="Maximum marks"
              type="number"
              min={1}
              error={errors.maxMarks?.message}
              {...register("maxMarks")}
            />
          </div>

          <div className="space-y-3 pt-1">
            <CheckboxField label="Accept late submissions" {...register("allowLateSubmission")} />
            <CheckboxField
              label="Allow updates before the deadline"
              {...register("allowUpdateBeforeDeadline")}
            />
          </div>

          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Saving…" : "Save changes"}
          </Button>
        </form>
      </Card>

      <ConfirmDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        onConfirm={() => void remove.mutateAsync()}
        title="Delete assignment"
        message={`Delete "${item.title}"? This cannot be undone. The API refuses if students have already submitted.`}
        confirmLabel="Delete"
      />
    </div>
  );
}