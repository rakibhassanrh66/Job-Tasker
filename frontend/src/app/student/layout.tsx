// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AppShell, type NavItem } from "@/components/app-shell";
import { RequireRole } from "@/components/require-role";
import { UserRole } from "@/lib/types";

const nav: NavItem[] = [
  { href: "/student/assignments", label: "Assignments" },
  { href: "/student/submissions", label: "My submissions" },
];

export default function StudentLayout({ children }: LayoutProps<"/student">) {
  return (
    <RequireRole roles={[UserRole.Student]}>
      <AppShell nav={nav}>{children}</AppShell>
    </RequireRole>
  );
}
