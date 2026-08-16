// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { Building2, Plus } from "lucide-react";
import { useState } from "react";
import { CreatePanel, type FieldSpec } from "@/components/create-panel";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Badge, Button, Card, PageHeader, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { useApiMutation, useApiPagedQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import type { ClassCourseDto, PagedResult } from "@/lib/types";

const PAGE_SIZE = 10;

const CLASS_FIELDS: FieldSpec[] = [
  { name: "name", label: "Class / Course Name", type: "text", required: true, placeholder: "e.g. Science Batch A" },
  { name: "code", label: "Code", type: "text", required: true, placeholder: "e.g. SCI-A", hint: "Short unique identifier, e.g. SCI-A." },
  { name: "description", label: "Description", type: "textarea", placeholder: "Optional context for this class…" },
];

export default function AdminClassesPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [createOpen, setCreateOpen] = useState(false);

  const { data } = useApiPagedQuery<ClassCourseDto>(
    ["admin", "classes"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch },
    () =>
      api.get<PagedResult<ClassCourseDto>>(
        `/classes${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const createClass = useApiMutation<unknown, Record<string, string>>({
    mutationFn: (values) =>
      api.post("/classes", {
        name: values.name,
        code: values.code,
        description: values.description,
      }),
    invalidate: [["admin", "classes"], ["admin", "classes", "count"]],
    successMessage: (_, values) => `${values.name} has been created.`,
  });

  const totalSubjects = rows.reduce((sum, row) => sum + row.subjectCount, 0);
  const totalEnrolments = rows.reduce((sum, row) => sum + row.enrollmentCount, 0);

  const columns: SortableColumn<ClassCourseDto>[] = [
    {
      key: "name",
      header: "Class / Course",
      sortValue: (row) => row.name.toLowerCase(),
      render: (row) => (
        <div>
          <p className="font-semibold text-white">{row.name}</p>
          <p className="mt-0.5 font-mono text-xs text-slate-500">{row.code}</p>
        </div>
      ),
    },
    {
      key: "subjectCount",
      header: "Subjects",
      sortValue: (row) => row.subjectCount,
      render: (row) => <Badge tone="blue">{row.subjectCount} bound</Badge>,
    },
    {
      key: "enrollmentCount",
      header: "Enrolments",
      sortValue: (row) => row.enrollmentCount,
      render: (row) => <Badge tone="green">{row.enrollmentCount} students</Badge>,
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="Classes & Courses"
        description="The containers every subject, assignment and enrolment hangs from."
        action={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden />
            New Class
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Total Classes" value={totalCount} tone="amber" icon={Building2} />
        <StatCard title="Subjects Bound" value={totalSubjects} tone="sky" />
        <StatCard title="Enrolled Students" value={totalEnrolments} tone="emerald" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="w-full max-w-md">
          <TextField
            label="Search Classes"
            placeholder="Search by name or code…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No classes found"
          emptyHint="Create the first class to start structuring the institution."
          emptyIcon={Building2}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <CreatePanel
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="New Class / Course"
        description="A class groups subjects and students; assignments publish against it."
        fields={CLASS_FIELDS}
        submitLabel="Create Class"
        onSubmit={createClass.mutateAsync}
      />
    </div>
  );
}