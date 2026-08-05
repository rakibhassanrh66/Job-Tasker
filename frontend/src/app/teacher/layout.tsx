// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AppShell, type NavItem } from "@/components/app-shell";
import { RequireRole } from "@/components/require-role";
import { UserRole } from "@/lib/types";

const nav: NavItem[] = [
  { href: "/teacher/assignments", label: "My assignments" },
  { href: "/teacher/assignments/new", label: "Create assignment" },
];

export default function TeacherLayout({ children }: LayoutProps<"/teacher">) {
  return (
    <RequireRole roles={[UserRole.Teacher]}>
      <AppShell nav={nav}>{children}</AppShell>
    </RequireRole>
  );
}
