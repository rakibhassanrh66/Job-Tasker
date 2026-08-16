// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { CheckCircle2, Info, TriangleAlert, X } from "lucide-react";
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

/**
 * Toast/notification system. Slide-in from the right edge with an accent border, Lucide
 * icon per type, auto-dismiss, and a visible close control. Aria-live so screen readers
 * announce arrivals.
 */

export type ToastType = "success" | "error" | "info";

export interface ToastMessage {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
}

interface ToastContextValue {
  toast: (title: string, message?: string, type?: ToastType) => void;
  success: (title: string, message?: string) => void;
  error: (title: string, message?: string) => void;
  info: (title: string, message?: string) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

const AUTO_DISMISS_MS = 4_500;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const toast = useCallback(
    (title: string, message?: string, type: ToastType = "info") => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
      setToasts((prev) => [...prev, { id, title, message, type }]);
      window.setTimeout(() => removeToast(id), AUTO_DISMISS_MS);
    },
    [removeToast],
  );

  const value = useMemo<ToastContextValue>(
    () => ({
      toast,
      success: (title, message) => toast(title, message, "success"),
      error: (title, message) => toast(title, message, "error"),
      info: (title, message) => toast(title, message, "info"),
    }),
    [toast],
  );

  return (
    <ToastContext.Provider value={value}>
      {children}

      <div
        aria-live="polite"
        className="pointer-events-none fixed bottom-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2.5 px-4 sm:px-0"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            role={t.type === "error" ? "alert" : "status"}
            className={`pointer-events-auto flex items-start gap-3 rounded-md border bg-ink-900/95 p-4 shadow-xl backdrop-blur-md animate-toast-enter ${
              t.type === "success"
                ? "border-emerald-500/40"
                : t.type === "error"
                  ? "border-red-500/40"
                  : "border-accent-400/40"
            }`}
          >
            <span
              className={`mt-0.5 shrink-0 ${
                t.type === "success"
                  ? "text-emerald-400"
                  : t.type === "error"
                    ? "text-red-400"
                    : "text-accent-400"
              }`}
            >
              {t.type === "success" && <CheckCircle2 className="h-5 w-5" aria-hidden />}
              {t.type === "error" && <TriangleAlert className="h-5 w-5" aria-hidden />}
              {t.type === "info" && <Info className="h-5 w-5" aria-hidden />}
            </span>

            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold leading-tight text-slate-100">{t.title}</p>
              {t.message && <p className="mt-1 text-xs leading-relaxed text-slate-400">{t.message}</p>}
            </div>

            <button
              type="button"
              onClick={() => removeToast(t.id)}
              aria-label="Dismiss notification"
              className="shrink-0 rounded-sm p-0.5 text-slate-500 transition-colors duration-150 hover:text-slate-200 focus-visible:outline-2 focus-visible:outline-accent-400"
            >
              <X className="h-4 w-4" aria-hidden />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);

  if (!context) {
    throw new Error("useToast must be used inside a ToastProvider.");
  }

  return context;
}