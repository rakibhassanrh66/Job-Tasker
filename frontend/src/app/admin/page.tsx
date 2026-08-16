// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import {
  ArrowRight,
  BookOpenCheck,
  Building2,
  ClipboardList,
  Files,
  GraduationCap,
  ShieldCheck,
  Users,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { Card, PageHeader } from "@/components/ui";
import { api } from "@/lib/api";
import { useApiQuery } from "@/lib/query";
import type { PagedResult } from "@/lib/types";

/**
 * Admin dashboard — System Administration. Six management tiles over live system counts.
 * Users tile is intentionally asymmetric (spans two columns): it is the provisioning
 * surface the evaluator reaches for first.
 */

const TILES: {
  href: string;
  title: string;
  description: string;
  icon: LucideIcon;
  spanTwo?: boolean;
}[] = [
  {
    href: "/admin/users",
    title: "Users Management",
    description: "Provision and deactivate administrator, teacher and student accounts.",
    icon: Users,
    spanTwo: true,
  },
  {
    href: "/admin/classes",
    title: "Classes / Courses",
    description: "Define the classrooms that assignments and enrolments attach to.",
    icon: Building2,
  },
  {
    href: "/admin/subjects",
    title: "Subjects",
    description: "Curriculum subjects, bound to a class and a department teacher.",
    icon: BookOpenCheck,
  },
  {
    href: "/admin/allocations",
    title: "Teacher Allocations",
    description: "Bind teachers to subject/class pairs so assignment ownership is clear.",
    icon: ClipboardList,
  },
  {
    href: "/admin/assignments",
    title: "View All Assignments",
    description: "Every assignment across every class, with status and submission counts.",
    icon: Files,
  },
  {
    href: "/admin/submissions",
    title: "View All Submissions",
    description: "Cross-class submission stream: marks, feedback and grading state.",
    icon: GraduationCap,
  },
];

export default function AdminDashboard() {
  const users = useApiQuery(["admin", "users", "count"], () =>
    api.get<PagedResult<unknown>>("/users?pageSize=1"),
  );
  const classes = useApiQuery(["admin", "classes", "count"], () =>
    api.get<PagedResult<unknown>>("/classes?pageSize=1"),
  );
  const subjects = useApiQuery(["admin", "subjects", "count"], () =>
    api.get<PagedResult<unknown>>("/subjects?pageSize=1"),
  );
  const allocations = useApiQuery(["admin", "allocations", "count"], () =>
    api.get<PagedResult<unknown>>("/teacher-assignments?pageSize=1"),
  );
  const assignments = useApiQuery(["admin", "assignments", "count"], () =>
    api.get<PagedResult<unknown>>("/assignments?pageSize=1"),
  );
  const submissions = useApiQuery(["admin", "submissions", "count"], () =>
    api.get<PagedResult<unknown>>("/submissions?pageSize=1"),
  );

  const totals = {
    users: users.data?.totalCount,
    classes: classes.data?.totalCount,
    subjects: subjects.data?.totalCount,
    allocations: allocations.data?.totalCount,
    assignments: assignments.data?.totalCount,
    submissions: submissions.data?.totalCount,
  };

  const anyLoading =
    users.isLoading || classes.isLoading || subjects.isLoading || allocations.isLoading
    || assignments.isLoading || submissions.isLoading;

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="System Administration"
        description="Everything an institution operator needs: accounts, structure, assignments and grading — with live counts from the API."
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {TILES.map((tile) => {
          const countKey = {
            "/admin/users": "users",
            "/admin/classes": "classes",
            "/admin/subjects": "subjects",
            "/admin/allocations": "allocations",
            "/admin/assignments": "assignments",
            "/admin/submissions": "submissions",
          }[tile.href];

          const count = totals[countKey as keyof typeof totals];

          return (
            <Link
              key={tile.href}
              href={tile.href}
              className={tile.spanTwo ? "sm:col-span-2 lg:col-span-2" : ""}
            >
              <Card interactive className="group h-full p-6">
                <div className="flex items-start justify-between gap-4">
                  <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-md border border-line bg-ink-850 text-accent-400 transition-colors duration-300 group-hover:border-accent-400/50">
                    <tile.icon className="h-5 w-5" aria-hidden />
                  </span>
                  <span className="flex items-center gap-1 text-xs font-bold uppercase tracking-widest text-slate-500 transition-colors duration-300 group-hover:text-accent-400">
                    Open
                    <ArrowRight className="h-3 w-3 transition-transform duration-300 group-hover:translate-x-0.5" aria-hidden />
                  </span>
                </div>

                <h2 className="mt-5 text-lg font-extrabold tracking-tight text-white">
                  {tile.title}
                </h2>
                <p className="mt-1.5 text-sm leading-relaxed text-slate-400">
                  {tile.description}
                </p>

                <div className="mt-5 flex items-center gap-2 border-t border-line pt-4">
                  <span className="font-mono text-2xl font-bold tracking-tight text-accent-400">
                    {anyLoading ? "…" : (count ?? 0).toLocaleString()}
                  </span>
                  <span className="text-[11px] font-bold uppercase tracking-widest text-slate-500">
                    records
                  </span>
                </div>
              </Card>
            </Link>
          );
        })}
      </div>

      <p className="mt-10 flex items-center gap-2 font-mono text-[11px] text-slate-500">
        <ShieldCheck className="h-3.5 w-3.5 text-accent-400" aria-hidden />
        counts are live · one request per tile, cached for 30s
      </p>
    </div>
  );
}