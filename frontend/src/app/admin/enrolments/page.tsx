// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { GraduationCap, UserMinus, Users as UsersIcon } from "lucide-react";
import { useState } from "react";
import { CreatePanel, type FieldSpec } from "@/components/create-panel";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { ConfirmDialog } from "@/components/modal";
import { Pagination } from "@/components/pagination";
import { Badge, Button, Card, PageHeader, SelectField, StatCard } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { useApiMutation, useApiPagedQuery, useApiQuery } from "@/lib/query";
import {
  UserRole,
  type ClassCourseDto,
  type EnrollmentDto,
  type PagedResult,
  type UserDto,
} from "@/lib/types";

const PAGE_SIZE = 10;

export default function AdminEnrolmentsPage() {
  const [page, setPage] = useState(1);
  const [classFilter, setClassFilter] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [pendingRemove, setPendingRemove] = useState<EnrollmentDto | null>(null);

  const students = useApiQuery<PagedResult<UserDto>>(
    ["admin", "users", "students"],
    () => api.get<PagedResult<UserDto>>(`/users?role=${UserRole.Student}&pageSize=100`),
  );

  const classes = useApiQuery<PagedResult<ClassCourseDto>>(
    ["admin", "classes", "options"],
    () => api.get<PagedResult<ClassCourseDto>>("/classes?pageSize=100"),
  );

  const { data } = useApiPagedQuery<EnrollmentDto>(
    ["admin", "enrolments"],
    { page, pageSize: PAGE_SIZE, classCourseId: classFilter || undefined },
    () =>
      api.get<PagedResult<EnrollmentDto>>(
        `/enrollments${query({ page, pageSize: PAGE_SIZE, classCourseId: classFilter || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const enrolFields: FieldSpec[] = [
    {
      name: "studentId",
      label: "Select Student",
      type: "select",
      required: true,
      options:
        students.data?.items.map((student) => ({
          value: student.id,
          label: `${student.fullName} — ${student.email}`,
        })) ?? [],
    },
    {
      name: "classCourseId",
      label: "Select Class / Cohort",
      type: "select",
      required: true,
      options:
        classes.data?.items.map((option) => ({
          value: option.id,
          label: `${option.code} — ${option.name}`,
        })) ?? [],
    },
  ];

  const createEnrolment = useApiMutation<unknown, Record<string, string>>({
    mutationFn: (values) =>
      api.post("/enrollments", {
        studentId: values.studentId,
        classCourseId: values.classCourseId,
      }),
    invalidate: [["admin", "enrolments"], ["admin", "classes"]],
    successMessage: "Student enrolled in class.",
  });

  const remove = useApiMutation<unknown, EnrollmentDto>({
    mutationFn: (row) => api.del(`/enrollments/${row.id}`),
    invalidate: [["admin", "enrolments"], ["admin", "classes"]],
    successMessage: (_, row) => `${row.studentName} removed from ${row.classCourseCode}.`,
  });

  const columns: SortableColumn<EnrollmentDto>[] = [
    {
      key: "studentName",
      header: "Student",
      sortValue: (row) => row.studentName.toLowerCase(),
      render: (row) => (
        <div className="flex items-center gap-3">
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-line bg-ink-850 text-[11px] font-bold text-slate-300">
            {row.studentName.slice(0, 2).toUpperCase()}
          </span>
          <div className="min-w-0">
            <p className="truncate font-semibold text-white">{row.studentName}</p>
            <p className="truncate font-mono text-xs text-slate-500">{row.studentEmail}</p>
          </div>
        </div>
      ),
    },
    {
      key: "classCourseCode",
      header: "Class",
      sortValue: (row) => row.classCourseCode.toLowerCase(),
      render: (row) => <Badge tone="green">{row.classCourseCode}</Badge>,
    },
    {
      key: "createdAt",
      header: "Enrolled",
      sortValue: (row) => row.createdAt,
      render: (row) => formatDate(row.createdAt),
      hideBelow: "sm",
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => (
        <Button variant="ghost" size="sm" onClick={() => setPendingRemove(row)}>
          <UserMinus className="h-3.5 w-3.5 text-red-400" aria-hidden />
          Remove
        </Button>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="Student Enrolments"
        description="Enrol students into classes to grant access to coursework and assignments."
        action={
          <Button onClick={() => setCreateOpen(true)}>
            <GraduationCap className="h-4 w-4" aria-hidden />
            Enrol Student
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2">
        <StatCard title="Active Enrolments" value={totalCount} tone="emerald" icon={UsersIcon} />
        <StatCard title="Registered Students" value={students.data?.totalCount ?? 0} tone="sky" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="w-full max-w-xs">
          <SelectField
            label="Filter by Class"
            value={classFilter}
            onChange={(e) => {
              setClassFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All Classes</option>
            {classes.data?.items.map((option) => (
              <option key={option.id} value={option.id}>
                {option.code} — {option.name}
              </option>
            ))}
          </SelectField>
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No enrolments found"
          emptyHint="Enrol a student into a class to get them access to assignments."
          emptyIcon={GraduationCap}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <CreatePanel
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Enrol Student in Class"
        description="The student will immediately see that class's published assignments."
        fields={enrolFields}
        submitLabel="Confirm Enrolment"
        onSubmit={createEnrolment.mutateAsync}
      />

      <ConfirmDialog
        open={pendingRemove !== null}
        onClose={() => setPendingRemove(null)}
        onConfirm={() => {
          if (pendingRemove) {
            void remove.mutateAsync(pendingRemove);
          }
        }}
        title="Remove enrolment"
        message={`Remove ${pendingRemove?.studentName ?? "this student"} from ${pendingRemove?.classCourseCode ?? "this class"}? They will lose sight of that class's assignments.`}
        confirmLabel="Remove"
      />
    </div>
  );
}