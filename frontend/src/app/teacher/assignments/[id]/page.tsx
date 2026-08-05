// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
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
import { AssignmentStatus, type AssignmentDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

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
  const [busy, setBusy] = useState(false);

  const assignment = useApi<AssignmentDto>(() => api.get<AssignmentDto>(`/assignments/${id}`), [id]);

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

  const onSubmit = handleSubmit(async (raw) => {
    const values = schema.parse(raw);
    setFailure(null);
    setSaved(false);

    try {
      await api.put<AssignmentDto>(`/assignments/${id}`, {
        title: values.title,
        description: values.description,
        deadline: fromLocalInputValue(values.deadline),
        maxMarks: values.maxMarks,
        allowLateSubmission: values.allowLateSubmission,
        allowUpdateBeforeDeadline: values.allowUpdateBeforeDeadline,
      });

      setSaved(true);
      assignment.reload();
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 422) {
        for (const [field, message] of Object.entries(cause.fieldErrors)) {
          if (field in schema.shape) {
            setError(field as keyof FormValues, { message });
          }
        }
      }

      setFailure(cause);
    }
  });

  const publish = async () => {
    setBusy(true);
    setFailure(null);

    try {
      await api.post(`/assignments/${id}/publish`);
      assignment.reload();
    } catch (cause) {
      setFailure(cause);
    } finally {
      setBusy(false);
    }
  };

  const remove = async () => {
    if (!window.confirm(`Delete "${item.title}"? This cannot be undone.`)) {
      return;
    }

    setBusy(true);
    setFailure(null);

    try {
      await api.del(`/assignments/${id}`);
      router.push("/teacher/assignments");
    } catch (cause) {
      // The API refuses when submissions exist, rather than cascading them away.
      setFailure(cause);
      setBusy(false);
    }
  };

  return (
    <>
      <PageHeader
        title={item.title}
        description={`${item.subjectName} · ${item.classCourseCode}`}
        action={
          <div className="flex flex-wrap gap-2">
            <Link href={`/teacher/assignments/${id}/submissions`}>
              <Button variant="secondary">Submissions ({item.submissionCount})</Button>
            </Link>
            {item.status === AssignmentStatus.Draft && (
              <Button disabled={busy} onClick={() => void publish()}>
                Publish
              </Button>
            )}
            <Button variant="ghost" disabled={busy} onClick={() => void remove()}>
              Delete
            </Button>
          </div>
        }
      />

      <div className="flex flex-wrap items-center gap-3 text-sm text-slate-600 dark:text-slate-400">
        <AssignmentStatusBadge status={item.status} />
        <span>Created {formatDateTime(item.createdAt)}</span>
        <span>·</span>
        <span>Updated {formatDateTime(item.updatedAt)}</span>
      </div>

      <Card className="max-w-2xl">
        <form onSubmit={onSubmit} className="space-y-4" noValidate>
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
    </>
  );
}
