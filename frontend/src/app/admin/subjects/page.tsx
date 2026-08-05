// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { CreatePanel } from "@/components/create-panel";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import { Button, Card, ErrorBanner, PageHeader, SelectField, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import type { ClassCourseDto, PagedResult, SubjectDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";

export default function AdminSubjectsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [classFilter, setClassFilter] = useState("");
  const [failure, setFailure] = useState<unknown>(null);
  const [draft, setDraft] = useState({ name: "", code: "", classCourseId: "" });

  // Every class, for the create form and the filter. A subject belongs to exactly one.
  const classes = useApi<PagedResult<ClassCourseDto>>(
    () => api.get<PagedResult<ClassCourseDto>>("/classes?pageSize=100"),
    [],
  );

  const { data, error, loading, reload } = useApi<PagedResult<SubjectDto>>(
    () =>
      api.get<PagedResult<SubjectDto>>(
        `/subjects${query({ page, pageSize: 10, search, classCourseId: classFilter || undefined })}`,
      ),
    [page, search, classFilter],
  );

  const remove = async (row: SubjectDto) => {
    if (!window.confirm(`Delete ${row.code}? This cannot be undone.`)) {
      return;
    }

    setFailure(null);

    try {
      await api.del(`/subjects/${row.id}`);
      reload();
    } catch (cause) {
      setFailure(cause);
    }
  };

  const columns: Column<SubjectDto>[] = [
    {
      header: "Subject",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.name}</p>
          <p className="font-mono text-xs text-slate-500">{row.code}</p>
        </div>
      ),
    },
    {
      header: "Class",
      cell: (row) => (
        <div>
          <p>{row.classCourseName}</p>
          <p className="font-mono text-xs text-slate-500">{row.classCourseCode}</p>
        </div>
      ),
    },
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
      <PageHeader title="Subjects" description="Subjects taught within a class." />

      <ErrorBanner error={failure} />

      <CreatePanel
        title="Add subject"
        submitLabel="Create subject"
        onSubmit={async () => {
          await api.post("/subjects", draft);
        }}
        onCreated={() => {
          setDraft({ name: "", code: "", classCourseId: "" });
          reload();
        }}
      >
        <TextField
          label="Name"
          placeholder="Data Structures"
          value={draft.name}
          onChange={(e) => setDraft({ ...draft, name: e.target.value })}
        />
        <TextField
          label="Code"
          placeholder="DS-101"
          value={draft.code}
          onChange={(e) => setDraft({ ...draft, code: e.target.value })}
        />
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
        <div className="grid gap-4 sm:grid-cols-2 lg:max-w-xl">
          <TextField
            label="Search"
            placeholder="Name or code"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
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
          empty="No subjects yet"
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
