// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AppShell, type NavItem } from "@/components/app-shell";
import { RequireRole } from "@/components/require-role";
import { UserRole } from "@/lib/types";

const nav: NavItem[] = [
  { href: "/admin/users", label: "Users" },
  { href: "/admin/classes", label: "Classes" },
  { href: "/admin/subjects", label: "Subjects" },
  { href: "/admin/allocations", label: "Teacher allocations" },
  { href: "/admin/enrolments", label: "Enrolments" },
  { href: "/admin/assignments", label: "Assignments" },
  { href: "/admin/submissions", label: "Submissions" },
];

export default function AdminLayout({ children }: LayoutProps<"/admin">) {
  return (
    <RequireRole roles={[UserRole.Admin]}>
      <AppShell nav={nav}>{children}</AppShell>
    </RequireRole>
  );
}
