// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ClipboardList, Plus } from "lucide-react";
import { useState } from "react";
import { CreatePanel, type FieldSpec } from "@/components/create-panel";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Badge, Button, Card, PageHeader, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { useApiMutation, useApiPagedQuery, useApiQuery } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import { UserRole, type ClassCourseDto, type PagedResult, type SubjectDto, type TeacherAssignmentDto, type UserDto } from "@/lib/types";

const PAGE_SIZE = 10;

export default function AdminAllocationsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [createOpen, setCreateOpen] = useState(false);

  const { data } = useApiPagedQuery<TeacherAssignmentDto>(
    ["admin", "allocations"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch },
    () =>
      api.get<PagedResult<TeacherAssignmentDto>>(
        `/teacher-assignments${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch })}`,
      ),
  );

  const { data: teacherData } = useApiQuery(["admin", "users", "teachers"], () =>
    api.get<PagedResult<UserDto>>(`/users?role=${UserRole.Teacher}&pageSize=100`),
  );
  const { data: subjectData } = useApiQuery(["admin", "subjects", "options"], () =>
    api.get<PagedResult<SubjectDto>>("/subjects?pageSize=100"),
  );
  const { data: classData } = useApiQuery(["admin", "classes", "options"], () =>
    api.get<PagedResult<ClassCourseDto>>("/classes?pageSize=100"),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const createAllocation = useApiMutation<unknown, Record<string, string>>({
    mutationFn: (values) =>
      api.post("/teacher-assignments", {
        teacherId: values.teacherId,
        subjectId: values.subjectId,
        classCourseId: values.classCourseId,
      }),
    invalidate: [["admin", "allocations"], ["admin", "allocations", "count"]],
    successMessage: () => "Allocation created.",
  });

  const allocationFields: FieldSpec[] = [
    {
      name: "teacherId",
      label: "Teacher",
      type: "select",
      required: true,
      options:
        teacherData?.items.map((teacher) => ({
          value: teacher.id,
          label: `${teacher.fullName} (${teacher.email})`,
        })) ?? [],
    },
    {
      name: "subjectId",
      label: "Subject",
      type: "select",
      required: true,
      options:
        subjectData?.items.map((subject) => ({
          value: subject.id,
          label: `${subject.name} (${subject.code})`,
        })) ?? [],
    },
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

  const columns: SortableColumn<TeacherAssignmentDto>[] = [
    {
      key: "teacherName",
      header: "Teacher",
      sortValue: (row) => row.teacherName.toLowerCase(),
      render: (row) => <p className="font-semibold text-white">{row.teacherName}</p>,
    },
    {
      key: "subjectName",
      header: "Subject",
      sortValue: (row) => row.subjectName.toLowerCase(),
      render: (row) => <Badge tone="amber">{row.subjectName}</Badge>,
    },
    {
      key: "classCourseCode",
      header: "Class / Course",
      sortValue: (row) => row.classCourseCode.toLowerCase(),
      render: (row) => <Badge tone="blue">{row.classCourseCode}</Badge>,
    },
  ];

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="Teacher Allocations"
        description="Bind a teacher to a subject inside a class. Only allocated teachers may own assignments there."
        action={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden />
            Allocate Teacher
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="Total Allocations" value={totalCount} tone="amber" icon={ClipboardList} />
        <StatCard title="Teachers Available" value={teacherData?.totalCount ?? 0} tone="sky" />
        <StatCard title="Subjects" value={subjectData?.totalCount ?? 0} tone="emerald" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="w-full max-w-md">
          <TextField
            label="Search Allocations"
            placeholder="Search by teacher or subject…"
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
          emptyTitle="No allocations yet"
          emptyHint="Allocate a teacher to a subject before they can create assignments."
          emptyIcon={ClipboardList}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <CreatePanel
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Allocate Teacher"
        description="Choose the teacher, subject and class for this allocation."
        fields={allocationFields}
        submitLabel="Create Allocation"
        onSubmit={createAllocation.mutateAsync}
      />
    </div>
  );
}