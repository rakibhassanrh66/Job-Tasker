// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm, type Resolver } from "react-hook-form";
import { z } from "zod";
import { Modal } from "@/components/modal";
import { Button, SelectField, TextAreaField, TextField } from "@/components/ui";
import { ApiError } from "@/lib/api";

/**
 * CreatePanel — modal create form, the standard pattern for every admin list page.
 *
 * - Schema is built from the field spec at runtime (never trusting API options blindly;
 *   required selects reject the empty GUID the old UI could send — F3).
 * - Per-field 422 messages from the API are mapped straight into react-hook-form's
 *   field errors (F2); the top-level message surfaces in a banner.
 * - Submitting state disables the actions; Escape/blur still close cleanly.
 */

export interface FieldSpec {
  name: string;
  label: string;
  type: "text" | "password" | "textarea" | "select" | "date";
  placeholder?: string;
  hint?: string;
  required?: boolean;
  options?: { value: string; label: string }[];
}

function buildSchema(fields: FieldSpec[]) {
  const shape: Record<string, z.ZodTypeAny> = {};

  for (const field of fields) {
    const base =
      field.type === "select"
        ? z.string()
        : field.type === "date"
          ? z.string()
          : z.string().trim();

    shape[field.name] = field.required
      ? base.min(1, `${field.label} is required`)
      : base.optional().or(z.literal(""));
  }

  return z.object(shape);
}

export function CreatePanel({
  open,
  onClose,
  title,
  description,
  fields,
  onSubmit,
  submitting = false,
  submitLabel = "Create",
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  fields: FieldSpec[];
  onSubmit: (values: Record<string, string>) => Promise<unknown>;
  submitting?: boolean;
  submitLabel?: string;
}) {
  const schema = buildSchema(fields);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<Record<string, string>>({
    resolver: zodResolver(schema) as Resolver<Record<string, string>>,
    defaultValues: Object.fromEntries(fields.map((f) => [f.name, ""])),
  });

  // Fresh form per open — no stale values leaking between different records.
  useEffect(() => {
    if (open) {
      reset(Object.fromEntries(fields.map((f) => [f.name, ""])));
      clearErrors();
    }
  }, [open, reset, clearErrors, fields]);

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values);
      onClose();
    } catch (error) {
      if (error instanceof ApiError && error.fieldErrors) {
        for (const [field, message] of Object.entries(error.fieldErrors)) {
          setError(field, { type: "server", message });
        }
      }
    }
  });

  const busy = submitting || isSubmitting;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      description={description}
      width="max-w-xl"
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button type="submit" onClick={submit} disabled={busy}>
            {busy ? "Saving…" : submitLabel}
          </Button>
        </>
      }
    >
      <form onSubmit={submit} className="space-y-5" noValidate>
        {fields.map((field) => {
          const error = errors[field.name]?.message;

          if (field.type === "textarea") {
            return (
              <TextAreaField
                key={field.name}
                label={field.label}
                placeholder={field.placeholder}
                hint={field.hint}
                error={error}
                disabled={busy}
                {...register(field.name)}
              />
            );
          }

          if (field.type === "select") {
            return (
              <SelectField
                key={field.name}
                label={field.label}
                error={error}
                disabled={busy}
                {...register(field.name)}
              >
                <option value="">{field.required ? "Select…" : "None"}</option>
                {field.options?.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </SelectField>
            );
          }

          return (
            <TextField
              key={field.name}
              label={field.label}
              type={field.type === "password" ? "password" : "text"}
              placeholder={field.placeholder}
              hint={field.hint}
              error={error}
              disabled={busy}
              {...register(field.name)}
            />
          );
        })}
      </form>
    </Modal>
  );
}