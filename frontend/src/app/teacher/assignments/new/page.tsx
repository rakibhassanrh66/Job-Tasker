// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ClipboardPlus } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  Button,
  Card,
  CheckboxField,
  EmptyState,
  ErrorBanner,
  PageHeader,
  SelectField,
  Spinner,
  TextAreaField,
  TextField,
} from "@/components/ui";
import { ApiError, api } from "@/lib/api";
import { fromLocalInputValue } from "@/lib/format";
import { useApiQuery } from "@/lib/query";
import type { AssignmentDto, PagedResult, TeacherAssignmentDto } from "@/lib/types";

/**
 * Mirrors CreateAssignmentRequestValidator. The deadline rule is the interesting one: the
 * server refuses a deadline in the past, because an assignment nobody could submit to is
 * not a useful thing to create.
 */
const schema = z.object({
  allocation: z.string().min(1, "Choose a subject and class."),
  title: z.string().min(1, "Title is required.").max(300),
  description: z.string().min(1, "Description is required.").max(5000),
  deadline: z
    .string()
    .min(1, "A deadline is required.")
    .refine((value) => new Date(value).getTime() > Date.now(), {
      message: "The deadline must be in the future.",
    }),
  maxMarks: z.coerce.number().int().min(1, "Maximum marks must be at least 1.").max(1000),
  allowLateSubmission: z.boolean(),
  allowUpdateBeforeDeadline: z.boolean(),
});

type FormValues = z.input<typeof schema>;

export default function NewAssignmentPage() {
  const router = useRouter();
  const [failure, setFailure] = useState<unknown>(null);

  // A teacher cannot read the subject or class catalogues — those are admin-only — so the
  // options come from their own allocations, which is also exactly the set rule 3 permits.
  const allocations = useApiQuery<PagedResult<TeacherAssignmentDto>>(
    ["teacher", "allocations", "mine"],
    () => api.get<PagedResult<TeacherAssignmentDto>>("/teacher-assignments/mine?pageSize=100"),
  );

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      allocation: "",
      title: "",
      description: "",
      deadline: "",
      maxMarks: 100,
      allowLateSubmission: false,
      allowUpdateBeforeDeadline: true,
    },
  });

  const onSubmit = handleSubmit(async (raw) => {
    const values = schema.parse(raw);
    setFailure(null);

    const chosen = allocations.data?.items.find((a) => a.id === values.allocation);

    if (!chosen) {
      setError("allocation", { message: "Choose a subject and class." });
      return;
    }

    try {
      const created = await api.post<AssignmentDto>("/assignments", {
        title: values.title,
        description: values.description,
        deadline: fromLocalInputValue(values.deadline),
        maxMarks: values.maxMarks,
        classCourseId: chosen.classCourseId,
        subjectId: chosen.subjectId,
        allowLateSubmission: values.allowLateSubmission,
        allowUpdateBeforeDeadline: values.allowUpdateBeforeDeadline,
      });

      router.push(`/teacher/assignments/${created.id}`);
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

  if (allocations.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner label="Loading your allocations" />
      </div>
    );
  }

  if (allocations.data && allocations.data.items.length === 0) {
    return (
      <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
        <PageHeader
          eyebrow="Teacher"
          title="New Assignment"
          description="Created as a draft. Students see nothing until you publish it."
        />
        <EmptyState
          icon={ClipboardPlus}
          title="You are not allocated to any subject yet"
          hint="An administrator has to allocate you to a subject within a class before you can set work there."
        />
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Teacher"
        title="New Assignment"
        description="Created as a draft. Students see nothing until you publish it."
      />

      <Card className="max-w-2xl p-6">
        <form onSubmit={onSubmit} className="space-y-5" noValidate>
          <ErrorBanner error={failure} />

          <SelectField
            label="Subject and class"
            hint="Only the subjects you are allocated to teach."
            error={errors.allocation?.message}
            {...register("allocation")}
          >
            <option value="">Choose…</option>
            {allocations.data?.items.map((allocation) => (
              <option key={allocation.id} value={allocation.id}>
                {allocation.subjectName} · {allocation.classCourseCode}
              </option>
            ))}
          </SelectField>

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
            <CheckboxField
              label="Accept late submissions"
              hint="Late work is accepted but permanently marked Late."
              {...register("allowLateSubmission")}
            />

            <CheckboxField
              label="Allow updates before the deadline"
              hint="Students can revise until the deadline, or until you grade the work."
              {...register("allowUpdateBeforeDeadline")}
            />
          </div>

          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creating…" : "Create draft"}
            </Button>
            <Button type="button" variant="secondary" onClick={() => router.back()}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}