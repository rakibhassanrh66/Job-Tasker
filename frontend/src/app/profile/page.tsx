// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useQueryClient } from "@tanstack/react-query";
import { BadgeCheck, KeyRound, LogOut, ShieldCheck, UserRound } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { AppShell } from "@/components/app-shell";
import { Badge, Button, Card, PageHeader, Skeleton } from "@/components/ui";
import { useAuth } from "@/lib/auth-context";
import { labelFor } from "@/lib/types";

const roleNames = {
  1: "Admin",
  2: "Teacher",
  3: "Student",
} as const;

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  return (parts[0]?.[0] ?? "U") + (parts.length > 1 ? parts[parts.length - 1][0] : "");
}

function ProfilePage() {
  const { user, ready, logout } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const tab = searchParams.get("tab") === "settings" ? "settings" : "overview";

  if (!ready) {
    return (
      <AppShell>
        <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
          <Skeleton className="h-8 w-56" />
          <div className="mt-8 grid gap-6 md:grid-cols-2">
            <Skeleton className="h-64" />
            <Skeleton className="h-64" />
          </div>
        </div>
      </AppShell>
    );
  }

  if (!user) {
    return null;
  }

  const handleLogout = () => {
    queryClient.clear();
    logout();
    router.replace("/login");
  };

  const setTab = (next: "overview" | "settings") => {
    router.replace(next === "settings" ? "/profile?tab=settings" : "/profile");
  };

  return (
    <AppShell>
      <div className="mx-auto w-full max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <PageHeader
          eyebrow="Account"
          title="Profile"
          description="Your identity in the Assignment & Submission Management System."
        />

        {/* Tab bar */}
        <div
          role="tablist"
          aria-label="Profile sections"
          className="mb-8 inline-flex rounded-md border border-line bg-ink-900/70 p-1"
        >
          <button
            type="button"
            role="tab"
            aria-selected={tab === "overview"}
            onClick={() => setTab("overview")}
            className={`inline-flex min-h-[44px] min-w-[44px] cursor-pointer items-center gap-2 rounded-sm px-4 text-sm font-semibold transition-colors duration-200 focus-visible:ring-2 focus-visible:ring-slate-500 ${
              tab === "overview"
                ? "bg-slate-100 text-slate-950"
                : "text-slate-400 hover:text-slate-100"
            }`}
          >
            <UserRound className="h-4 w-4" aria-hidden />
            Overview
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={tab === "settings"}
            onClick={() => setTab("settings")}
            className={`inline-flex min-h-[44px] min-w-[44px] cursor-pointer items-center gap-2 rounded-sm px-4 text-sm font-semibold transition-colors duration-200 focus-visible:ring-2 focus-visible:ring-slate-500 ${
              tab === "settings"
                ? "bg-slate-100 text-slate-950"
                : "text-slate-400 hover:text-slate-100"
            }`}
          >
            <KeyRound className="h-4 w-4" aria-hidden />
            Settings
          </button>
        </div>

        {tab === "overview" ? (
          <div className="grid gap-6 md:grid-cols-2">
            {/* Identity */}
            <Card className="p-6">
              <div className="flex items-center gap-4">
                <span className="flex h-14 w-14 items-center justify-center rounded-full border border-line bg-ink-800 text-lg font-bold text-slate-100">
                  {initialsOf(user.fullName)}
                </span>
                <div className="min-w-0">
                  <h2 className="truncate text-lg font-extrabold tracking-tight text-white">
                    {user.fullName}
                  </h2>
                  <p className="truncate font-mono text-xs text-slate-500">{user.email}</p>
                </div>
              </div>

              <dl className="mt-6 space-y-3 border-t border-line pt-6 text-sm">
                <div className="flex items-center justify-between gap-4">
                  <dt className="text-slate-500">Role</dt>
                  <dd className="flex items-center gap-2 font-semibold text-slate-200">
                    {labelFor(roleNames, user.role)}
                    <Badge tone="amber">@{user.role}</Badge>
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className="text-slate-500">Account ID</dt>
                  <dd className="font-mono text-xs text-slate-300">{user.id}</dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className="text-slate-500">Status</dt>
                  <dd className="flex items-center gap-1.5 font-semibold text-emerald-300">
                    <span className="inline-block h-1.5 w-1.5 rounded-full bg-emerald-400" />
                    {user.isActive ? "Active" : "Suspended"}
                  </dd>
                </div>
              </dl>
            </Card>

            {/* What you can do */}
            <Card className="p-6">
              <h3 className="text-sm font-bold uppercase tracking-widest text-slate-500">
                Your workspace
              </h3>
              <p className="mt-3 text-sm leading-relaxed text-slate-300">
                You are signed in with <span className="text-slate-100">{user.fullName}</span> as
                a{" "}
                <span className="text-slate-100">
                  {labelFor(roleNames, user.role).toLowerCase()}
                </span>
                . Every screen you can reach is listed in the top navigation; the backend
                enforces the same role boundaries on every request, so nothing is hidden by the
                UI alone.
              </p>
              <div className="mt-5 flex flex-wrap gap-2">
                <Button variant="secondary" size="sm" onClick={() => setTab("settings")}>
                  <KeyRound className="h-3.5 w-3.5" aria-hidden />
                  Session &amp; security
                </Button>
              </div>
            </Card>
          </div>
        ) : (
          <div className="grid gap-6 md:grid-cols-2">
            {/* Session */}
            <Card className="p-6">
              <h3 className="flex items-center gap-2 text-sm font-bold uppercase tracking-widest text-slate-500">
                <ShieldCheck className="h-4 w-4 text-accent-400" aria-hidden />
                Session
              </h3>
              <ul className="mt-4 space-y-2.5 text-sm text-slate-300">
                <li className="flex items-start gap-2.5">
                  <BadgeCheck className="mt-0.5 h-4 w-4 shrink-0 text-accent-400" aria-hidden />
                  Signed in as {user.email}
                </li>
                <li className="flex items-start gap-2.5">
                  <BadgeCheck className="mt-0.5 h-4 w-4 shrink-0 text-accent-400" aria-hidden />
                  JWT session with silent refresh — expired tokens renew in the background.
                </li>
                <li className="flex items-start gap-2.5">
                  <BadgeCheck className="mt-0.5 h-4 w-4 shrink-0 text-accent-400" aria-hidden />
                  Login attempts are throttled and locked out server-side after repeated
                  failures.
                </li>
              </ul>
            </Card>

            {/* Sign out */}
            <Card className="p-6">
              <h3 className="text-sm font-bold uppercase tracking-widest text-slate-500">
                Sign out
              </h3>
              <p className="mt-3 text-sm leading-relaxed text-slate-300">
                Ends this session: the access token, refresh token and the session marker are
                wiped locally, and you are returned to the sign-in screen.
              </p>
              <Button variant="danger" className="mt-5" onClick={handleLogout}>
                <LogOut className="h-4 w-4" aria-hidden />
                Sign out of this device
              </Button>
            </Card>
          </div>
        )}
      </div>
    </AppShell>
  );
}

export default function ProfilePageRoute() {
  return (
    <Suspense fallback={null}>
      <ProfilePage />
    </Suspense>
  );
}