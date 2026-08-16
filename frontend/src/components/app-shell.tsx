// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { Breadcrumb } from "@/components/layout/Breadcrumb";
import { TopNav } from "@/components/layout/TopNav";

/**
 * AppShell — the shared chrome around every protected page.
 *
 * TopNav owns the header (role-aware links, avatar menu, logout, mobile overlay) and
 * Breadcrumb derives the trail from the URL, so a page only has to render its own
 * content. The guard itself is enforced server-side in the API and by the edge
 * middleware; the shell just never shows a role what it cannot touch.
 */
export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <TopNav />

      <Breadcrumb />

      <main className="flex-1">{children}</main>

      {/* Footer */}
      <footer className="border-t border-line">
        <div className="mx-auto flex w-full max-w-7xl flex-col items-center justify-between gap-2 px-4 py-8 text-xs text-slate-500 sm:flex-row sm:px-6">
          <p>
            Assignment &amp; Submission Management System — evaluation build by Rakib Hassan for
            OnnoRokom Projukti Ltd.
          </p>
          <p className="font-mono">candidacy · 2026</p>
        </div>
      </footer>
    </div>
  );
}