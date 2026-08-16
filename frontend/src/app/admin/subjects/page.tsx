// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { BookOpenCheck, Plus } from "lucide-react";
import { useState } from "react";
import { CreatePanel, type FieldSpec } from "@/components/create-panel";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Badge, Button, Card, PageHeader, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { useApiMutation, useApiPagedQuery, useApiQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import type { ClassCourseDto, PagedResult, SubjectDto } from "@/lib/types";

const PAGE_SIZE = 10;

export default function AdminSubjectsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [createOpen, setCreateOpen] = useState(false);

  const { data } = useApiPagedQuery<SubjectDto>(
    ["admin", "subjects"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch },
    () =>
      api.get<PagedResult<SubjectDto>>(
        `/subjects${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch })}`,
      ),
  );

  const { data: classData } = useApiQuery(["admin", "classes", "options"], () =>
    api.get<PagedResult<ClassCourseDto>>("/classes?pageSize=100"),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const createSubject = useApiMutation<unknown, Record<string, string>>({
    mutationFn: (values) =>
      api.post("/subjects", {
        name: values.name,
        code: values.code,
        classCourseId: values.classCourseId,
      }),
    invalidate: [["admin", "subjects"], ["admin", "subjects", "count"], ["admin", "classes"]],
    successMessage: (_, values) => `${values.name} has been created.`,
  });

  const subjectFields: FieldSpec[] = [
    { name: "name", label: "Subject Name", type: "text", required: true, placeholder: "e.g. Physics" },
    { name: "code", label: "Code", type: "text", required: true, placeholder: "e.g. PHY" },
    {
      name: "classCourseId",
      label: "Class / Course",
      type: "select",
      required: true,
      options:
        classData?.items.map((classCourse) => ({
          value: classCourse.id,
          label: `${classCourse.name} (${classCourse.code})`,
        })) ?? [],
    },
  ];

  const columns: SortableColumn<SubjectDto>[] = [
    {
      key: "name",
      header: "Subject",
      sortValue: (row) => row.name.toLowerCase(),
      render: (row) => (
        <div>
          <p className="font-semibold text-white">{row.name}</p>
          <p className="mt-0.5 font-mono text-xs text-slate-500">{row.code}</p>
        </div>
      ),
    },
    {
      key: "classCourseName",
      header: "Class / Course",
      sortValue: (row) => row.classCourseName.toLowerCase(),
      render: (row) => (
        <Badge tone="blue">
          {row.classCourseName} ({row.classCourseCode})
        </Badge>
      ),
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="Subjects"
        description="The curriculum units that teachers are allocated to teach."
        action={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden />
            New Subject
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2">
        <StatCard title="Total Subjects" value={totalCount} tone="amber" icon={BookOpenCheck} />
        <StatCard title="Available Classes" value={classData?.totalCount ?? 0} tone="sky" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="w-full max-w-md">
          <TextField
            label="Search Subjects"
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
          emptyTitle="No subjects found"
          emptyHint="Create a subject and bind it to a class."
          emptyIcon={BookOpenCheck}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <CreatePanel
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="New Subject"
        description="Subjects belong to exactly one class; teachers are allocated to them next."
        fields={subjectFields}
        submitLabel="Create Subject"
        onSubmit={createSubject.mutateAsync}
      />
    </div>
  );
}