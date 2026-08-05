// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { CreatePanel } from "@/components/create-panel";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Button, Card, ErrorBanner, PageHeader, SelectField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { UserRole, type PagedResult, type SubjectDto, type TeacherAssignmentDto, type UserDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

/**
 * Which teacher teaches which subject in which class.
 *
 * This table is the input to business rule 3 — it decides where a teacher may create work —
 * so it is admin-only. A teacher who could edit it could grant themselves permission to set
 * assignments anywhere.
 */
export default function AdminAllocationsPage() {
  const [page, setPage] = useState(1);
  const [teacherFilter, setTeacherFilter] = useState("");
  const [failure, setFailure] = useState<unknown>(null);
  const [draft, setDraft] = useState({ teacherId: "", subjectId: "" });

  const teachers = useApi<PagedResult<UserDto>>(
    () => api.get<PagedResult<UserDto>>(`/users${query({ role: UserRole.Teacher, pageSize: 100 })}`),
    [],
  );

  const subjects = useApi<PagedResult<SubjectDto>>(
    () => api.get<PagedResult<SubjectDto>>("/subjects?pageSize=100"),
    [],
  );

  const { data, error, loading, reload } = useApi<PagedResult<TeacherAssignmentDto>>(
    () =>
      api.get<PagedResult<TeacherAssignmentDto>>(
        `/teacher-assignments${query({ page, pageSize: 10, teacherId: teacherFilter || undefined })}`,
      ),
    [page, teacherFilter],
  );

  const remove = async (row: TeacherAssignmentDto) => {
    if (
      !window.confirm(
        `Remove ${row.teacherName} from ${row.subjectName} (${row.classCourseCode})? `
        + "They will no longer be able to create assignments there.",
      )
    ) {
      return;
    }

    setFailure(null);

    try {
      await api.del(`/teacher-assignments/${row.id}`);
      reload();
    } catch (cause) {
      setFailure(cause);
    }
  };

  const columns: Column<TeacherAssignmentDto>[] = [
    { header: "Teacher", cell: (row) => row.teacherName },
    { header: "Subject", cell: (row) => row.subjectName },
    { header: "Class", cell: (row) => <span className="font-mono text-xs">{row.classCourseCode}</span> },
    {
      header: "",
      align: "right",
      cell: (row) => (
        <Button variant="ghost" onClick={() => void remove(row)}>
          Remove
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Teacher allocations"
        description="Which teacher may set work in which subject and class."
      />

      <ErrorBanner error={failure} />

      <CreatePanel
        title="Add allocation"
        submitLabel="Allocate"
        onSubmit={async () => {
          // The class is implied by the subject: a subject belongs to exactly one class,
          // and the API rejects a pair that disagrees. Deriving it here means the admin
          // cannot construct that mismatch in the first place.
          const subject = subjects.data?.items.find((s) => s.id === draft.subjectId);

          await api.post("/teacher-assignments", {
            teacherId: draft.teacherId,
            subjectId: draft.subjectId,
            classCourseId: subject?.classCourseId,
          });
        }}
        onCreated={() => {
          setDraft({ teacherId: "", subjectId: "" });
          reload();
        }}
      >
        <SelectField
          label="Teacher"
          value={draft.teacherId}
          onChange={(e) => setDraft({ ...draft, teacherId: e.target.value })}
        >
          <option value="">Choose…</option>
          {teachers.data?.items.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.fullName} — {teacher.email}
            </option>
          ))}
        </SelectField>

        <SelectField
          label="Subject and class"
          value={draft.subjectId}
          onChange={(e) => setDraft({ ...draft, subjectId: e.target.value })}
        >
          <option value="">Choose…</option>
          {subjects.data?.items.map((subject) => (
            <option key={subject.id} value={subject.id}>
              {subject.name} · {subject.classCourseCode}
            </option>
          ))}
        </SelectField>
      </CreatePanel>

      <Card className="space-y-4">
        <div className="max-w-sm">
          <SelectField
            label="Teacher"
            value={teacherFilter}
            onChange={(e) => {
              setTeacherFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All teachers</option>
            {teachers.data?.items.map((teacher) => (
              <option key={teacher.id} value={teacher.id}>
                {teacher.fullName}
              </option>
            ))}
          </SelectField>
        </div>

        <DataTable
          rows={data?.items}
          columns={columns}
          loading={loading}
          error={error}
          rowKey={(row) => row.id}
          empty="No allocations yet"
          emptyHint="A teacher cannot create assignments until they are allocated to a subject."
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
