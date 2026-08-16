// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AppShell } from "@/components/app-shell";
import { RequireRole } from "@/components/require-role";
import { UserRole } from "@/lib/types";

export default function TeacherLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireRole roles={[UserRole.Teacher]}>
      <AppShell>{children}</AppShell>
    </RequireRole>
  );
}