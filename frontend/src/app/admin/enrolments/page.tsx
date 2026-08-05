// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { CreatePanel } from "@/components/create-panel";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Button, Card, ErrorBanner, PageHeader, SelectField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { UserRole, type ClassCourseDto, type EnrollmentDto, type PagedResult, type UserDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

/**
 * Class membership. This is what scopes the work a student can see (business rule 2), so
 * like allocations it is admin-only — a student who could enrol themselves could read any
 * class's assignments.
 *
 * Spelled "enrolments" in the UI and "enrollments" on the API route: the backend route was
 * fixed before this screen existed and changing a published path for spelling is not worth
 * the churn.
 */
export default function AdminEnrolmentsPage() {
  const [page, setPage] = useState(1);
  const [classFilter, setClassFilter] = useState("");
  const [failure, setFailure] = useState<unknown>(null);
  const [draft, setDraft] = useState({ studentId: "", classCourseId: "" });

  const students = useApi<PagedResult<UserDto>>(
    () => api.get<PagedResult<UserDto>>(`/users${query({ role: UserRole.Student, pageSize: 100 })}`),
    [],
  );

  const classes = useApi<PagedResult<ClassCourseDto>>(
    () => api.get<PagedResult<ClassCourseDto>>("/classes?pageSize=100"),
    [],
  );

  const { data, error, loading, reload } = useApi<PagedResult<EnrollmentDto>>(
    () =>
      api.get<PagedResult<EnrollmentDto>>(
        `/enrollments${query({ page, pageSize: 10, classCourseId: classFilter || undefined })}`,
      ),
    [page, classFilter],
  );

  const remove = async (row: EnrollmentDto) => {
    if (
      !window.confirm(
        `Remove ${row.studentName} from ${row.classCourseCode}? `
        + "They will lose sight of that class's assignments.",
      )
    ) {
      return;
    }

    setFailure(null);

    try {
      await api.del(`/enrollments/${row.id}`);
      reload();
    } catch (cause) {
      setFailure(cause);
    }
  };

  const columns: Column<EnrollmentDto>[] = [
    {
      header: "Student",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.studentName}</p>
          <p className="text-xs text-slate-500">{row.studentEmail}</p>
        </div>
      ),
    },
    { header: "Class", cell: (row) => <span className="font-mono text-xs">{row.classCourseCode}</span> },
    { header: "Enrolled", secondary: true, cell: (row) => formatDate(row.createdAt) },
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
        title="Enrolments"
        description="Which students belong to which class. This decides what work they can see."
      />

      <ErrorBanner error={failure} />

      <CreatePanel
        title="Add enrolment"
        submitLabel="Enrol student"
        onSubmit={async () => {
          await api.post("/enrollments", draft);
        }}
        onCreated={() => {
          setDraft({ studentId: "", classCourseId: "" });
          reload();
        }}
      >
        <SelectField
          label="Student"
          value={draft.studentId}
          onChange={(e) => setDraft({ ...draft, studentId: e.target.value })}
        >
          <option value="">Choose…</option>
          {students.data?.items.map((student) => (
            <option key={student.id} value={student.id}>
              {student.fullName} — {student.email}
            </option>
          ))}
        </SelectField>

        <SelectField
          label="Class"
          value={draft.classCourseId}
          onChange={(e) => setDraft({ ...draft, classCourseId: e.target.value })}
        >
          <option value="">Choose…</option>
          {classes.data?.items.map((option) => (
            <option key={option.id} value={option.id}>
              {option.code} — {option.name}
            </option>
          ))}
        </SelectField>
      </CreatePanel>

      <Card className="space-y-4">
        <div className="max-w-sm">
          <SelectField
            label="Class"
            value={classFilter}
            onChange={(e) => {
              setClassFilter(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All classes</option>
            {classes.data?.items.map((option) => (
              <option key={option.id} value={option.id}>
                {option.code}
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
          empty="No enrolments yet"
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
