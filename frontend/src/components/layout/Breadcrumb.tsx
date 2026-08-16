// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { roleHome } from "@/lib/types";

/**
 * Breadcrumb — Home / Section / Current, derived from the URL so no page has to wire it.
 * The last segment is the current page (plain text); every earlier segment is a link.
 * Rendered by the app shell, so it appears on every page except the standalone login.
 */

const SEGMENT_LABELS: Record<string, string> = {
  admin: "Admin",
  teacher: "Teacher",
  student: "Student",
  users: "Users",
  classes: "Classes & Courses",
  subjects: "Subjects",
  allocations: "Teacher Allocations",
  enrolments: "Enrolments",
  assignments: "Assignments",
  submissions: "Submissions",
  new: "New Assignment",
  profile: "Profile",
};

function labelForSegment(segment: string): string {
  return SEGMENT_LABELS[segment] ?? segment.charAt(0).toUpperCase() + segment.slice(1);
}

export function Breadcrumb() {
  const pathname = usePathname();
  const { user } = useAuth();

  const segments = pathname.split("/").filter(Boolean);

  if (segments.length === 0) {
    return null;
  }

  const homeHref = user ? roleHome[user.role] : "/login";

  // Build cumulative paths so each segment links to a real destination.
  const crumbs = segments.map((segment, index) => ({
    label: labelForSegment(segment),
    href: `/${segments.slice(0, index + 1).join("/")}`,
    isCurrent: index === segments.length - 1,
  }));

  return (
    <nav aria-label="Breadcrumb" className="w-full max-w-7xl px-4 pt-6 sm:px-6 lg:px-8">
      <ol className="flex flex-wrap items-center gap-1.5 text-sm text-slate-500">
        <li>
          <Link
            href={homeHref}
            className="transition-colors duration-150 hover:text-slate-200 focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950"
          >
            Home
          </Link>
        </li>

        {crumbs.map((crumb) => (
          <li key={crumb.href} className="flex items-center gap-1.5">
            <span className="text-slate-500" aria-hidden>
              /
            </span>
            {crumb.isCurrent ? (
              <span aria-current="page" className="font-medium text-slate-300">
                {crumb.label}
              </span>
            ) : (
              <Link
                href={crumb.href}
                className="transition-colors duration-150 hover:text-slate-200 focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950"
              >
                {crumb.label}
              </Link>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}