// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { ApiError } from "@/lib/api";

/**
 * The shared vocabulary every screen is built from.
 *
 * Kept in one file because these are small and always used together; splitting them into
 * a file each would mean seven imports at the top of every page for no benefit.
 */

// ---------------------------------------------------------------------------------
// Buttons
// ---------------------------------------------------------------------------------

type ButtonVariant = "primary" | "secondary" | "danger" | "ghost";

const buttonStyles: Record<ButtonVariant, string> = {
  primary: "bg-slate-900 text-white hover:bg-slate-700 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-white",
  secondary: "border border-slate-300 bg-white text-slate-800 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:hover:bg-slate-700",
  danger: "bg-red-600 text-white hover:bg-red-700",
  ghost: "text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white",
};

export function Button({
  variant = "primary",
  className = "",
  children,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: ButtonVariant }) {
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded-md px-3.5 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${buttonStyles[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}

// ---------------------------------------------------------------------------------
// Form fields
// ---------------------------------------------------------------------------------

const fieldClass =
  "w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 outline-none placeholder:text-slate-400 focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:focus:ring-slate-700";

function FieldShell({
  label,
  error,
  hint,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <label className="block space-y-1.5">
      <span className="block text-sm font-medium text-slate-700 dark:text-slate-200">{label}</span>
      {children}
      {hint && !error && <span className="block text-xs text-slate-500">{hint}</span>}
      {error && (
        <span role="alert" className="block text-xs font-medium text-red-600">
          {error}
        </span>
      )}
    </label>
  );
}

export function TextField({
  label,
  error,
  hint,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string; hint?: string }) {
  return (
    <FieldShell label={label} error={error} hint={hint}>
      <input
        className={`${fieldClass} ${error ? "border-red-400" : ""}`}
        aria-invalid={error ? true : undefined}
        {...props}
      />
    </FieldShell>
  );
}

export function TextAreaField({
  label,
  error,
  hint,
  ...props
}: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; error?: string; hint?: string }) {
  return (
    <FieldShell label={label} error={error} hint={hint}>
      <textarea
        className={`${fieldClass} min-h-28 ${error ? "border-red-400" : ""}`}
        aria-invalid={error ? true : undefined}
        {...props}
      />
    </FieldShell>
  );
}

export function SelectField({
  label,
  error,
  hint,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { label: string; error?: string; hint?: string }) {
  return (
    <FieldShell label={label} error={error} hint={hint}>
      <select className={`${fieldClass} ${error ? "border-red-400" : ""}`} {...props}>
        {children}
      </select>
    </FieldShell>
  );
}

export function CheckboxField({
  label,
  hint,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string }) {
  return (
    <label className="flex items-start gap-2.5">
      <input
        type="checkbox"
        className="mt-0.5 h-4 w-4 rounded border-slate-300 accent-slate-900 dark:accent-slate-200"
        {...props}
      />
      <span>
        <span className="block text-sm font-medium text-slate-700 dark:text-slate-200">{label}</span>
        {hint && <span className="block text-xs text-slate-500">{hint}</span>}
      </span>
    </label>
  );
}

// ---------------------------------------------------------------------------------
// Feedback
// ---------------------------------------------------------------------------------

/**
 * Renders whatever went wrong.
 *
 * Prefers the server's own words: it answers ProblemDetails with a title and a detail
 * written for the caller, so inventing a friendlier message here would usually be less
 * informative. The traceId is shown because it is the only thing that ties a user's report
 * to a line in the log.
 */
export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) {
    return null;
  }

  const isApi = error instanceof ApiError;
  const title = isApi ? error.title : "Something went wrong";
  const detail = isApi ? error.detail : error instanceof Error ? error.message : String(error);

  return (
    <div role="alert" className="rounded-md border border-red-200 bg-red-50 p-3.5 dark:border-red-900 dark:bg-red-950">
      <p className="text-sm font-semibold text-red-800 dark:text-red-200">{title}</p>
      {detail && <p className="mt-1 text-sm text-red-700 dark:text-red-300">{detail}</p>}
      {isApi && error.traceId && (
        <p className="mt-1.5 font-mono text-[11px] text-red-500">trace {error.traceId}</p>
      )}
    </div>
  );
}

export function SuccessBanner({ children }: { children: ReactNode }) {
  return (
    <div role="status" className="rounded-md border border-emerald-200 bg-emerald-50 p-3.5 text-sm text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200">
      {children}
    </div>
  );
}

export function Spinner({ label }: { label?: string }) {
  return (
    <span className="inline-flex items-center gap-2.5 text-sm text-slate-500">
      <span className="h-4 w-4 animate-spin rounded-full border-2 border-slate-300 border-t-slate-700" />
      {label ?? "Loading"}
    </span>
  );
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <div className="rounded-md border border-dashed border-slate-300 p-8 text-center dark:border-slate-700">
      <p className="text-sm font-medium text-slate-700 dark:text-slate-200">{title}</p>
      {hint && <p className="mt-1 text-sm text-slate-500">{hint}</p>}
    </div>
  );
}

// ---------------------------------------------------------------------------------
// Layout
// ---------------------------------------------------------------------------------

export function PageHeader({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">{title}</h1>
        {description && <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">{description}</p>}
      </div>
      {action}
    </div>
  );
}

export function Card({ children, className = "" }: { children: ReactNode; className?: string }) {
  return (
    <div className={`rounded-lg border border-slate-200 bg-white p-5 dark:border-slate-700 dark:bg-slate-900 ${className}`}>
      {children}
    </div>
  );
}

// ---------------------------------------------------------------------------------
// Badges
// ---------------------------------------------------------------------------------

export function Badge({ children, tone = "neutral" }: { children: ReactNode; tone?: "neutral" | "green" | "amber" | "red" | "blue" }) {
  const tones = {
    neutral: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300",
    green: "bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300",
    amber: "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
    red: "bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300",
    blue: "bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300",
  };

  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap ${tones[tone]}`}>
      {children}
    </span>
  );
}
