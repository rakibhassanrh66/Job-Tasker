// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useState } from "react";
import { CreatePanel } from "@/components/create-panel";
import { DataTable, type Column } from "@/components/data-table";
import { Pagination } from "@/components/pagination";
import {
  Badge,
  Button,
  Card,
  ErrorBanner,
  PageHeader,
  SelectField,
  TextField,
} from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { UserRole, roleLabels, type PagedResult, type UserDto } from "@/lib/types";
import { useApi } from "@/lib/use-api";
import { useAuth } from "@/lib/auth-context";

export default function AdminUsersPage() {
  const { user: me } = useAuth();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const [failure, setFailure] = useState<unknown>(null);

  const [draft, setDraft] = useState({
    fullName: "",
    email: "",
    password: "",
    role: String(UserRole.Student),
  });

  const { data, error, loading, reload } = useApi<PagedResult<UserDto>>(
    () =>
      api.get<PagedResult<UserDto>>(
        `/users${query({ page, pageSize: 10, search, role: role || undefined })}`,
      ),
    [page, search, role],
  );

  const deactivate = async (row: UserDto) => {
    if (!window.confirm(`Deactivate ${row.fullName}? They will no longer be able to sign in.`)) {
      return;
    }

    setFailure(null);

    try {
      await api.del(`/users/${row.id}`);
      reload();
    } catch (cause) {
      setFailure(cause);
    }
  };

  const columns: Column<UserDto>[] = [
    {
      header: "Name",
      cell: (row) => (
        <div>
          <p className="font-medium text-slate-900 dark:text-white">{row.fullName}</p>
          <p className="text-xs text-slate-500">{row.email}</p>
        </div>
      ),
    },
    { header: "Role", cell: (row) => <Badge>{roleLabels[row.role]}</Badge> },
    {
      header: "Active",
      cell: (row) =>
        row.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>,
    },
    { header: "Created", secondary: true, cell: (row) => formatDate(row.createdAt) },
    {
      header: "",
      align: "right",
      cell: (row) =>
        // The API refuses to let an admin deactivate themselves, so the button is not
        // offered rather than offered and then refused.
        row.isActive && row.id !== me?.id ? (
          <Button variant="ghost" onClick={() => void deactivate(row)}>
            Deactivate
          </Button>
        ) : null,
    },
  ];

  return (
    <>
      <PageHeader title="Users" description="Accounts for administrators, teachers and students." />

      <ErrorBanner error={failure} />

      <CreatePanel
        title="Add user"
        submitLabel="Create user"
        onSubmit={async () => {
          await api.post("/users", { ...draft, role: Number(draft.role) });
        }}
        onCreated={() => {
          setDraft({ fullName: "", email: "", password: "", role: String(UserRole.Student) });
          reload();
        }}
      >
        <TextField
          label="Full name"
          value={draft.fullName}
          onChange={(e) => setDraft({ ...draft, fullName: e.target.value })}
        />
        <TextField
          label="Email"
          type="email"
          value={draft.email}
          onChange={(e) => setDraft({ ...draft, email: e.target.value })}
        />
        <TextField
          label="Password"
          type="password"
          hint="At least 8 characters, with an uppercase letter, a lowercase letter and a digit."
          value={draft.password}
          onChange={(e) => setDraft({ ...draft, password: e.target.value })}
        />
        <SelectField
          label="Role"
          value={draft.role}
          onChange={(e) => setDraft({ ...draft, role: e.target.value })}
        >
          {Object.values(UserRole).map((value) => (
            <option key={value} value={value}>
              {roleLabels[value]}
            </option>
          ))}
        </SelectField>
      </CreatePanel>

      <Card className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2 lg:max-w-xl">
          <TextField
            label="Search"
            placeholder="Name or email"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <SelectField
            label="Role"
            value={role}
            onChange={(e) => {
              setRole(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All roles</option>
            {Object.values(UserRole).map((value) => (
              <option key={value} value={value}>
                {roleLabels[value]}
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
          empty="No users match"
        />

        {data && <Pagination page={data} onPageChange={setPage} />}
      </Card>
    </>
  );
}
