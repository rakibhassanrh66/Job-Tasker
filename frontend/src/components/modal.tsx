// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AnimatePresence, motion } from "motion/react";
import { X } from "lucide-react";
import { useEffect, useRef, type ReactNode } from "react";
import { Button } from "@/components/ui";

/**
 * Modal + ConfirmDialog.
 *
 * Backdrop fades in over 0.2s; the panel springs in from scale 0.95 / y 20 over 0.3s,
 * and both reverse on exit. Own focus trapping (Tab cycles within the dialog, Escape
 * closes), locks body scroll while open, and restores focus to the trigger on close.
 * Rendered with fixed positioning so transform ancestors (page-transition wrappers)
 * can't break it.
 */

export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  width = "max-w-lg",
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children?: ReactNode;
  footer?: ReactNode;
  width?: string;
}) {
  const dialogRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    const previouslyFocused = document.activeElement as HTMLElement | null;

    document.body.style.overflow = "hidden";

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    const onFocus = (event: FocusEvent) => {
      const container = dialogRef.current;

      if (container && !container.contains(event.target as Node)) {
        const focusables = container.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])',
        );

        if (focusables.length > 0) {
          focusables[0].focus();
        }
      }
    };

    document.addEventListener("keydown", onKeyDown);
    document.addEventListener("focusin", onFocus);

    return () => {
      document.body.style.overflow = "";
      document.removeEventListener("keydown", onKeyDown);
      document.removeEventListener("focusin", onFocus);
      previouslyFocused?.focus();
    };
  }, [open, onClose]);

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          className="fixed inset-0 z-40 flex items-start justify-center overflow-y-auto p-4 sm:p-8"
          role="dialog"
          aria-modal="true"
          aria-labelledby="modal-title"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
        >
          {/* Backdrop */}
          <motion.div
            className="fixed inset-0 bg-ink-950/70 backdrop-blur-sm"
            onClick={onClose}
            aria-hidden
          />

          {/* Panel — springs in (scale 0.95 → 1, y 20 → 0) per the motion guidelines */}
          <motion.div
            ref={dialogRef}
            tabIndex={-1}
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", stiffness: 300, damping: 30 }}
            className={`relative z-10 my-8 w-full ${width} rounded-md border border-line-strong bg-ink-900 shadow-2xl shadow-black/60`}
          >
            <header className="flex items-start justify-between gap-4 border-b border-line px-6 py-4">
              <div>
                <h2 id="modal-title" className="text-lg font-extrabold tracking-tight text-white">
                  {title}
                </h2>
                {description && <p className="mt-1 text-sm text-slate-400">{description}</p>}
              </div>
              <button
                type="button"
                onClick={onClose}
                aria-label="Close dialog"
                className="rounded-sm p-1 text-slate-500 transition-colors duration-150 hover:bg-ink-800 hover:text-slate-200 focus-visible:outline-2 focus-visible:outline-slate-500"
              >
                <X className="h-5 w-5" aria-hidden />
              </button>
            </header>

            {children && <div className="max-h-[60vh] overflow-y-auto px-6 py-5">{children}</div>}

            {footer && (
              <footer className="flex flex-wrap items-center justify-end gap-2 border-t border-line bg-ink-850/50 px-6 py-4">
                {footer}
              </footer>
            )}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  message,
  confirmLabel = "Confirm",
  tone = "danger",
}: {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmLabel?: string;
  tone?: "danger" | "primary";
}) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      width="max-w-sm"
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button
            variant={tone === "danger" ? "danger" : "primary"}
            onClick={() => {
              onConfirm();
              onClose();
            }}
          >
            {confirmLabel}
          </Button>
        </>
      }
    >
      <p className="text-sm leading-relaxed text-slate-300">{message}</p>
    </Modal>
  );
}