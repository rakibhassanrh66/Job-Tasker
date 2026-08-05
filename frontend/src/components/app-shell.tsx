// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { roleLabels } from "@/lib/types";
import { Button } from "./ui";

export interface NavItem {
  href: string;
  label: string;
}

/**
 * Header, navigation and sign-out, shared by all three role areas.
 *
 * The nav is passed in rather than derived from the role here, so each role's layout owns
 * its own list and adding a screen means editing one file.
 */
export function AppShell({ nav, children }: { nav: NavItem[]; children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  const signOut = () => {
    logout();
    router.replace("/login");
  };

  return (
    <div className="flex min-h-full flex-col">
      <header className="border-b border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
          <div className="flex items-center gap-3">
            <span className="text-sm font-semibold tracking-tight">Assignment System</span>
            {user && (
              <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                {roleLabels[user.role]}
              </span>
            )}
          </div>

          <div className="flex items-center gap-3">
            {user && (
              <span className="hidden text-sm text-slate-600 sm:inline dark:text-slate-400">
                {user.fullName}
              </span>
            )}
            <Button variant="secondary" onClick={signOut}>
              Sign out
            </Button>
          </div>
        </div>

        <nav aria-label="Sections" className="mx-auto max-w-6xl px-4">
          <ul className="-mb-px flex flex-wrap gap-1 overflow-x-auto">
            {nav.map((item) => {
              // startsWith so a detail page keeps its section tab lit.
              const active = pathname === item.href || pathname.startsWith(`${item.href}/`);

              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    aria-current={active ? "page" : undefined}
                    className={`inline-block border-b-2 px-3 py-2.5 text-sm whitespace-nowrap transition-colors ${
                      active
                        ? "border-slate-900 font-medium text-slate-900 dark:border-white dark:text-white"
                        : "border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-800 dark:hover:text-slate-200"
                    }`}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 space-y-6 px-4 py-8">{children}</main>

      <footer className="border-t border-slate-200 py-4 dark:border-slate-800">
        <p className="mx-auto max-w-6xl px-4 text-xs text-slate-500">
          © 2026 Rakib Hassan · Evaluation build — not licensed for production use
        </p>
      </footer>
    </div>
  );
}
