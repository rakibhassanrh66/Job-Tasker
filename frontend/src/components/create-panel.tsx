// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState, type ReactNode } from "react";
import { Button, Card, ErrorBanner, SuccessBanner } from "./ui";

/**
 * The "add one of these" card that sits above every admin list.
 *
 * Collapsed by default so the list — the thing an admin came for — is not pushed below the
 * fold by a form they may not need. Owns the open/closed, error and success state so the
 * five admin screens do not each reimplement it.
 */
export function CreatePanel({
  title,
  submitLabel,
  onSubmit,
  onCreated,
  children,
}: {
  title: string;
  submitLabel: string;
  onSubmit: () => Promise<void>;
  onCreated: () => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState<unknown>(null);
  const [done, setDone] = useState(false);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setFailure(null);
    setDone(false);

    try {
      await onSubmit();
      setDone(true);
      onCreated();
    } catch (cause) {
      setFailure(cause);
    } finally {
      setBusy(false);
    }
  };

  if (!open) {
    return (
      <div className="flex justify-end">
        <Button onClick={() => setOpen(true)}>{title}</Button>
      </div>
    );
  }

  return (
    <Card>
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-medium text-slate-900 dark:text-white">{title}</h2>
          <Button
            type="button"
            variant="ghost"
            onClick={() => {
              setOpen(false);
              setFailure(null);
              setDone(false);
            }}
          >
            Cancel
          </Button>
        </div>

        {done && <SuccessBanner>Created.</SuccessBanner>}
        <ErrorBanner error={failure} />

        <div className="grid gap-4 sm:grid-cols-2">{children}</div>

        <Button type="submit" disabled={busy}>
          {busy ? "Saving…" : submitLabel}
        </Button>
      </form>
    </Card>
  );
}
