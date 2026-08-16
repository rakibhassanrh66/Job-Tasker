// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { labelFor } from "@/lib/types";
import type { AssignmentStatus, SubmissionStatus, UserRole } from "@/lib/types";
import { Badge } from "@/components/ui";

/**
 * Status badges. One tone per enum value, driven by the numeric enum contracts in
 * lib/types — every branch uses labelFor so an out-of-range value can never render
 * "undefined".
 */

export const roleLabels: Record<UserRole, string> = {
  [1]: "Admin",
  [2]: "Teacher",
  [3]: "Student",
};

export const assignmentStatusLabels: Record<AssignmentStatus, string> = {
  [1]: "Draft",
  [2]: "Published",
  [3]: "Archived",
};

export const submissionStatusLabels: Record<SubmissionStatus, string> = {
  [1]: "Submitted",
  [2]: "Under Review",
  [3]: "Graded",
  [4]: "Returned",
  [5]: "Late",
};

export function RoleBadge({ role }: { role: number }) {
  const label = labelFor(roleLabels, role);

  return (
    <Badge tone={role === 1 ? "red" : role === 2 ? "amber" : "blue"}>{label}</Badge>
  );
}

export function AssignmentStatusBadge({ status }: { status: number }) {
  const tone =
    status === 1 ? "neutral" : status === 2 ? "amber" : "purple";

  return <Badge tone={tone}>{labelFor(assignmentStatusLabels, status)}</Badge>;
}

export function SubmissionStatusBadge({ status }: { status: number }) {
  const tone =
    status === 1 || status === 5 ? "blue" : status === 2 ? "amber" : status === 3 ? "green" : "neutral";

  return <Badge tone={tone}>{labelFor(submissionStatusLabels, status)}</Badge>;
}