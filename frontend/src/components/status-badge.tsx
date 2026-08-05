// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import {
  AssignmentStatus,
  SubmissionStatus,
  assignmentStatusLabels,
  submissionStatusLabels,
} from "@/lib/types";
import { Badge } from "./ui";

/** The two enums the API sends as integers, rendered as words with a consistent colour. */

export function AssignmentStatusBadge({ status }: { status: AssignmentStatus }) {
  const tone =
    status === AssignmentStatus.Published
      ? "green"
      : status === AssignmentStatus.Draft
        ? "amber"
        : "neutral";

  return <Badge tone={tone}>{assignmentStatusLabels[status]}</Badge>;
}

export function SubmissionStatusBadge({ status }: { status: SubmissionStatus }) {
  const tones = {
    [SubmissionStatus.Submitted]: "blue",
    [SubmissionStatus.UnderReview]: "amber",
    [SubmissionStatus.Graded]: "green",
    [SubmissionStatus.Returned]: "neutral",
    // Late is its own colour rather than sharing Submitted's: the whole point of the
    // status is that it stays distinguishable from work that arrived on time.
    [SubmissionStatus.Late]: "red",
  } as const;

  return <Badge tone={tones[status]}>{submissionStatusLabels[status]}</Badge>;
}
