// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { Loader2, TriangleAlert, type LucideIcon } from "lucide-react";
import React, {
  type ButtonHTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import { ApiError } from "@/lib/api";

// ---------------------------------------------------------------------------------
// Buttons
// ---------------------------------------------------------------------------------

type ButtonVariant = "primary" | "secondary" | "danger" | "ghost" | "success" | "accent";
type ButtonSize = "sm" | "md" | "lg";

const buttonStyles: Record<ButtonVariant, string> = {
  // Primary CTA: slate-100 on the ink ramp — the enforced "bg-slate-100 text-slate-950".
  primary:
    "bg-slate-100 text-slate-950 hover:bg-slate-200 active:bg-slate-300 "
    + "hover:shadow-[0_4px_24px_rgba(241,245,249,0.15)]",
  accent:
    "bg-slate-100 text-slate-950 hover:bg-slate-200 active:bg-slate-300 "
    + "hover:shadow-[0_4px_24px_rgba(241,245,249,0.15)]",
  secondary:
    "border border-line-strong bg-ink-850 text-slate-200 hover:border-ink-500 hover:bg-ink-800 "
    + "hover:text-white active:bg-ink-700",
  danger:
    "bg-red-500/15 border border-red-500/40 text-red-300 hover:bg-red-500/25 hover:border-red-400/60 "
    + "active:bg-red-500/35",
  ghost: "text-slate-400 hover:bg-ink-800 hover:text-slate-100 active:bg-ink-700",
  success:
    "bg-emerald-500/15 border border-emerald-500/40 text-emerald-300 hover:bg-emerald-500/25 "
    + "hover:border-emerald-400/60 active:bg-emerald-500/35",
};

const buttonSizes: Record<ButtonSize, string> = {
  sm: "h-8 px-3 text-xs",
  md: "h-10 px-4 text-sm",
  lg: "h-12 px-6 text-base",
};

export function Button({
  variant = "primary",
  size = "md",
  loading = false,
  className = "",
  children,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: ButtonSize;
  /** Shows a spinning Loader2 in place of the label and blocks interaction. */
  loading?: boolean;
}) {
  const isDisabled = disabled || loading;

  return (
    <button
      className={`inline-flex cursor-pointer select-none items-center justify-center gap-2 rounded-md font-semibold outline-none transition-all duration-300 ease-expo focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950 disabled:cursor-not-allowed hover:scale-[1.03] active:scale-[0.98] ${
        isDisabled ? "pointer-events-none opacity-70" : ""
      } ${buttonStyles[variant]} ${buttonSizes[size]} ${className}`}
      disabled={isDisabled}
      {...props}
    >
      {loading && <Loader2 className="h-4 w-4 animate-spin" aria-hidden />}
      {children}
    </button>
  );
}

// ---------------------------------------------------------------------------------
// Form fields — visible labels (never placeholder-only), focus ring with the amber glow
// ---------------------------------------------------------------------------------

const fieldClass =
  "w-full rounded-md border border-line bg-ink-850 px-3.5 py-2.5 text-sm text-slate-100 "
  + "outline-none placeholder:text-slate-500 transition-all duration-300 ease-expo "
  + "hover:border-ink-500 focus:border-slate-500 focus:ring-2 focus:ring-slate-500/20 "
  + "disabled:cursor-not-allowed disabled:opacity-50";

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
      <span className="block text-[11px] font-semibold uppercase tracking-widest text-slate-500">
        {label}
      </span>
      {children}
      {hint && !error && <span className="block text-xs text-slate-500">{hint}</span>}
      {error && (
        <span role="alert" className="block text-xs font-medium text-red-400">
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
        className={`${fieldClass} ${error ? "border-red-500/60 focus:border-red-400 focus:ring-red-400/20" : ""}`}
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
        className={`${fieldClass} min-h-28 ${error ? "border-red-500/60 focus:border-red-400 focus:ring-red-400/20" : ""}`}
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
      <select
        className={`${fieldClass} appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%2212%22%20height%3D%2212%22%20viewBox%3D%220%200%2024%2024%22%20fill%3D%22none%22%20stroke%3D%22%2394a3b8%22%20stroke-width%3D%222%22%3E%3Cpath%20d%3D%22m6%209%206%206%206-6%22%2F%3E%3C%2Fsvg%3E')] bg-[right_0.9rem_center] bg-no-repeat pr-9 ${error ? "border-red-500/60 focus:border-red-400 focus:ring-red-400/20" : ""}`}
        {...props}
      >
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
    <label className="flex cursor-pointer items-start gap-3">
      <input
        type="checkbox"
        className="mt-0.5 h-4 w-4 cursor-pointer rounded-sm border-line-strong bg-ink-850 accent-accent-400 focus-visible:outline-2 focus-visible:outline-slate-500"
        {...props}
      />
      <span className="select-none">
        <span className="block text-sm font-medium text-slate-200">{label}</span>
        {hint && <span className="block text-xs text-slate-500">{hint}</span>}
      </span>
    </label>
  );
}

// ---------------------------------------------------------------------------------
// Feedback & banners
// ---------------------------------------------------------------------------------

export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) {
    return null;
  }

  const isApi = error instanceof ApiError;
  const title = isApi ? error.title : error instanceof Error ? error.message : String(error);
  const detail = isApi ? error.detail : undefined;
  const fieldErrors = isApi ? error.fieldErrors : undefined;

  return (
    <div
      role="alert"
      className="rounded-md border border-red-500/40 bg-red-950/30 p-4 animate-fade-in"
    >
      <div className="flex gap-3">
        <TriangleAlert className="mt-0.5 h-5 w-5 shrink-0 text-red-400" aria-hidden />
        <div className="min-w-0">
          <p className="text-sm font-semibold text-red-200">{title}</p>

          {detail && <p className="mt-1 text-sm text-red-300">{detail}</p>}

          {fieldErrors && Object.keys(fieldErrors).length > 0 && (
            <ul className="mt-2 space-y-0.5">
              {Object.entries(fieldErrors).map(([field, message]) => (
                <li key={field} className="font-mono text-xs text-red-300">
                  {field}: {message}
                </li>
              ))}
            </ul>
          )}

          {isApi && error.traceId && (
            <p className="mt-2 font-mono text-[11px] text-red-400/80">trace {error.traceId}</p>
          )}
        </div>
      </div>
    </div>
  );
}

export function SuccessBanner({ children }: { children: ReactNode }) {
  return (
    <div
      role="status"
      className="rounded-md border border-emerald-500/40 bg-emerald-950/30 px-4 py-3 text-sm font-medium text-emerald-200"
    >
      {children}
    </div>
  );
}

export function Spinner({ label }: { label?: string }) {
  return (
    <span className="inline-flex items-center gap-2.5 text-sm text-slate-400">
      <span className="h-4 w-4 animate-spin rounded-full border-2 border-ink-600 border-t-accent-400" />
      {label ?? "Loading"}
    </span>
  );
}

export function EmptyState({
  title,
  hint,
  action,
  icon: Icon,
}: {
  title: string;
  hint?: string;
  action?: ReactNode;
  icon?: LucideIcon;
}) {
  const IconComponent = Icon ?? TriangleAlert;

  return (
    <div className="flex flex-col items-center justify-center rounded-md border border-dashed border-ink-600 p-12 text-center">
      <span className="mb-3 flex h-12 w-12 items-center justify-center rounded-md border border-line bg-ink-850 text-slate-500">
        <IconComponent className="h-6 w-6" aria-hidden />
      </span>
      <p className="text-base font-semibold text-slate-200">{title}</p>
      {hint && <p className="mt-1 max-w-sm text-sm text-slate-500">{hint}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}

export function Skeleton({ className = "" }: { className?: string }) {
  return <div className={`animate-pulse rounded-lg bg-slate-800 ${className}`} aria-hidden />;
}

// ---------------------------------------------------------------------------------
// Layout & cards
// ---------------------------------------------------------------------------------

export function PageHeader({
  eyebrow,
  title,
  description,
  action,
  align = "left",
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: ReactNode;
  align?: "left" | "center";
}) {
  const centered = align === "center";

  return (
    <div
      className={`flex flex-col gap-4 ${centered ? "items-center text-center" : "items-start"} ${
        centered ? "py-24" : "py-16"
      }`}
    >
      <div className="w-full">
        {eyebrow && (
          <p
            className={`text-[11px] font-bold uppercase tracking-[0.25em] text-accent-400 ${
              centered ? "text-center" : ""
            }`}
          >
            {eyebrow}
          </p>
        )}

        <h1
          className={`mt-3 text-4xl font-extrabold tracking-tight text-white sm:text-5xl ${
            centered ? "text-center" : ""
          }`}
        >
          {title}
        </h1>

        {description && (
          <p
            className={`mt-4 max-w-2xl text-base leading-relaxed text-slate-400 ${
              centered ? "mx-auto text-center" : ""
            }`}
          >
            {description}
          </p>
        )}
      </div>

      {action && <div className="flex shrink-0 flex-wrap items-center gap-2">{action}</div>}
    </div>
  );
}

export function Card({
  children,
  className = "",
  interactive = true,
}: {
  children: ReactNode;
  className?: string;
  /**
   * Every card lifts on hover — translateY(-6px), border brightens to slate-700 and a
   * soft shadow drops in. Set false only where the lift would fight the layout.
   */
  interactive?: boolean;
}) {
  return (
    <div
      className={`rounded-md border border-line bg-ink-900/70 backdrop-blur-sm ${
        interactive
          ? "transition-all duration-300 ease-expo hover:-translate-y-1.5 hover:border-line-strong hover:shadow-[0_8px_30px_rgba(0,0,0,0.3)]"
          : ""
      } ${className}`}
    >
      {children}
    </div>
  );
}

// ---------------------------------------------------------------------------------
// StatCard — the big-number readouts on every dashboard
// ---------------------------------------------------------------------------------

const statTones = {
  amber: { text: "text-accent-400", line: "border-accent-400/40" },
  emerald: { text: "text-emerald-400", line: "border-emerald-500/40" },
  sky: { text: "text-sky-400", line: "border-sky-500/40" },
  violet: { text: "text-violet-400", line: "border-violet-500/40" },
  red: { text: "text-red-400", line: "border-red-500/40" },
  slate: { text: "text-slate-300", line: "border-line" },
} as const;

export function StatCard({
  title,
  value,
  description,
  tone = "slate",
  icon: Icon,
}: {
  title: string;
  value: string | number;
  description?: string;
  tone?: keyof typeof statTones;
  icon?: LucideIcon;
}) {
  const toneStyles = statTones[tone];

  return (
    <Card className={`p-5 ${toneStyles.line}`}>
      <div className="flex items-center justify-between gap-3">
        <p className="text-[11px] font-bold uppercase tracking-widest text-slate-500">{title}</p>
        {Icon && <Icon className={`h-4 w-4 ${toneStyles.text}`} aria-hidden />}
      </div>
      <p className="mt-2 text-3xl font-extrabold tracking-tight text-white sm:text-4xl">
        {value}
      </p>
      {description && <p className="mt-1 text-xs text-slate-500">{description}</p>}
    </Card>
  );
}

// ---------------------------------------------------------------------------------
// Badges
// ---------------------------------------------------------------------------------

const badgeTones = {
  neutral: "border-line bg-ink-850 text-slate-300",
  green: "border-emerald-500/40 bg-emerald-950/40 text-emerald-300",
  amber: "border-accent-400/40 bg-amber-950/40 text-amber-300",
  red: "border-red-500/40 bg-red-950/40 text-red-300",
  blue: "border-sky-500/40 bg-sky-950/40 text-sky-300",
  purple: "border-violet-500/40 bg-violet-950/40 text-violet-300",
} as const;

export function Badge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: keyof typeof badgeTones;
}) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-md border px-2.5 py-0.5 text-xs font-semibold ${badgeTones[tone]}`}
    >
      {children}
    </span>
  );
}

// ---------------------------------------------------------------------------------
// Progress bar
// ---------------------------------------------------------------------------------

const progressTones = {
  amber: "bg-accent-400",
  emerald: "bg-emerald-500",
  sky: "bg-sky-500",
  red: "bg-red-500",
} as const;

export function ProgressBar({
  value,
  max = 100,
  tone = "amber",
  label,
}: {
  value: number;
  max?: number;
  tone?: keyof typeof progressTones;
  label?: string;
}) {
  const percentage = Math.min(100, Math.max(0, Math.round((value / max) * 100)));

  return (
    <div className="w-full space-y-1.5">
      {label && (
        <div className="flex justify-between text-xs font-medium text-slate-400">
          <span>{label}</span>
          <span>{percentage}%</span>
        </div>
      )}
      <div className="h-1.5 w-full overflow-hidden rounded-full bg-ink-800">
        <div
          className={`h-full rounded-full transition-all duration-300 ease-expo ${progressTones[tone]}`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}