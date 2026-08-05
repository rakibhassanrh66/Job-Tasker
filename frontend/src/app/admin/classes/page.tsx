// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { CreatePanel } from "@/components/create-panel";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Button, Card, ErrorBanner, PageHeader, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import type { ClassCourseDto, PagedResult } from "@/lib/types";
import { useApi } from "@/lib/use-api";

export default function AdminClassesPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [failure, setFailure] = useState<unknown>(null);
  const [draft, setDraft] = useState({ name: "", code: "" });

  const { data, error, loading, reload } = useApi<PagedResult<ClassCourseDto>>(
    () => api.get<PagedResult<ClassCourseDto>>(`/classes${query({ page, pageSize: 10, search })}`),
    [page, search],
  );

  const remove = async (row: ClassCourseDto) => {
    // The API refuses when the class still has enrolments, rather than cascading students
    // away with it — so say that up front.
    const warning =
      row.enrollmentCount > 0
        ? `${row.code} has ${row.enrollmentCount} enrolment(s) and cannot be deleted while they exist.`
        : `Delete ${row.code}? This cannot be undone.`;

    if (!window.confirm(warning)) {
      return;
    }

    setFailure(null);

    try {
      await api.del(`/classes/${row.id}`);
      reload();
    } catch (cause) {
      setFailure(cause);
    }
  };

  const columns: Column<ClassCourseDto>[] = [
    {
      header: "Class",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.name}</p>
          <p className="font-mono text-xs text-slate-500">{row.code}</p>
        </div>
      ),
    },
    { header: "Subjects", align: "right", cell: (row) => row.subjectCount },
    { header: "Students", align: "right", cell: (row) => row.enrollmentCount },
    {
      header: "",
      align: "right",
      cell: (row) => (
        <Button variant="ghost" onClick={() => void remove(row)}>
          Delete
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader title="Classes" description="Classes and courses students are enrolled into." />

      <ErrorBanner error={failure} />

      <CreatePanel
        title="Add class"
        submitLabel="Create class"
        onSubmit={async () => {
          await api.post("/classes", draft);
        }}
        onCreated={() => {
          setDraft({ name: "", code: "" });
          reload();
        }}
      >
        <TextField
          label="Name"
          placeholder="Computer Science 101"
          value={draft.name}
          onChange={(e) => setDraft({ ...draft, name: e.target.value })}
        />
        <TextField
          label="Code"
          placeholder="CS-101"
          hint="Must be unique."
          value={draft.code}
          onChange={(e) => setDraft({ ...draft, code: e.target.value })}
        />
      </CreatePanel>

      <Card className="space-y-4">
        <div className="max-w-sm">
          <TextField
            label="Search"
            placeholder="Name or code"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <DataTable
          rows={data?.items}
          columns={columns}
          loading={loading}
          error={error}
          rowKey={(row) => row.id}
          empty="No classes yet"
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
