// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { UserX, Users as UsersIcon } from "lucide-react";
import { useState } from "react";
import { CreatePanel, type FieldSpec } from "@/components/create-panel";
import { SortableDataTable, type SortableColumn } from "@/components/data-table";
import { ConfirmDialog } from "@/components/modal";
import { Pagination } from "@/components/pagination";
import { RoleBadge } from "@/components/status-badge";
import { Badge, Button, Card, PageHeader, SelectField, StatCard, TextField } from "@/components/ui";
import { api, query } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { useApiPagedQuery, useApiMutation } from "@/lib/query";
import { useDebouncedValue } from "@/lib/use-debounced";
import { useAuth } from "@/lib/auth-context";
import { roleLabels, UserRole, type PagedResult, type UserDto } from "@/lib/types";

const PAGE_SIZE = 10;

const USER_FIELDS: FieldSpec[] = [
  { name: "fullName", label: "Full Name", type: "text", required: true, placeholder: "e.g. Sarah Jenkins" },
  { name: "email", label: "Email Address", type: "text", required: true, placeholder: "name@school.edu" },
  {
    name: "password",
    label: "Initial Password",
    type: "password",
    required: true,
    hint: "At least 8 characters, containing uppercase, lowercase, and a number.",
  },
  {
    name: "role",
    label: "System Role",
    type: "select",
    required: true,
    options: [
      { value: String(UserRole.Student), label: "Student" },
      { value: String(UserRole.Teacher), label: "Teacher" },
      { value: String(UserRole.Admin), label: "Admin" },
    ],
  },
];

export default function AdminUsersPage() {
  const { user: me } = useAuth();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  const [createOpen, setCreateOpen] = useState(false);
  const [pendingDeactivate, setPendingDeactivate] = useState<UserDto | null>(null);

  const { data } = useApiPagedQuery<UserDto>(
    ["admin", "users"],
    { page, pageSize: PAGE_SIZE, search: debouncedSearch, role: role || undefined },
    () =>
      api.get<PagedResult<UserDto>>(
        `/users${query({ page, pageSize: PAGE_SIZE, search: debouncedSearch, role: role || undefined })}`,
      ),
  );

  const rows = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const createUser = useApiMutation<unknown, Record<string, string>>({
    mutationFn: (values) =>
      api.post("/users", {
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        role: Number(values.role),
      }),
    invalidate: [["admin", "users"]],
    successMessage: (_, values) => `${values.fullName} has been registered.`,
  });

  const deactivate = useApiMutation<unknown, UserDto>({
    mutationFn: (row) => api.del(`/users/${row.id}`),
    invalidate: [["admin", "users"]],
    successMessage: (_, row) => `${row.fullName} is now deactivated.`,
  });

  const columns: SortableColumn<UserDto>[] = [
    {
      key: "fullName",
      header: "User Details",
      sortValue: (row) => row.fullName.toLowerCase(),
      render: (row) => (
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md border border-line bg-ink-850 text-xs font-bold text-slate-300">
            {row.fullName.slice(0, 2).toUpperCase()}
          </span>
          <div className="min-w-0">
            <p className="truncate font-semibold text-white">{row.fullName}</p>
            <p className="truncate font-mono text-xs text-slate-500">{row.email}</p>
          </div>
        </div>
      ),
    },
    {
      key: "role",
      header: "Role",
      sortValue: (row) => roleLabels[row.role],
      render: (row) => <RoleBadge role={row.role} />,
    },
    {
      key: "isActive",
      header: "Status",
      sortValue: (row) => (row.isActive ? 1 : 0),
      render: (row) =>
        row.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>,
    },
    {
      key: "createdAt",
      header: "Registered",
      sortValue: (row) => row.createdAt,
      render: (row) => formatDate(row.createdAt),
      hideBelow: "sm",
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) =>
        row.isActive && row.id !== me?.id ? (
          <Button variant="ghost" size="sm" onClick={() => setPendingDeactivate(row)}>
            <UserX className="h-3.5 w-3.5" aria-hidden />
            Deactivate
          </Button>
        ) : null,
    },
  ];

  const adminCount = rows.filter((row) => row.role === UserRole.Admin).length;
  const teacherCount = rows.filter((row) => row.role === UserRole.Teacher).length;
  const studentCount = rows.filter((row) => row.role === UserRole.Student).length;

  return (
    <div className="mx-auto w-full max-w-7xl px-4 sm:px-6">
      <PageHeader
        eyebrow="Administrator"
        title="User Management"
        description="Provision accounts, assign roles, and cut off access when someone leaves."
        action={
          <Button onClick={() => setCreateOpen(true)}>
            <UsersIcon className="h-4 w-4" aria-hidden />
            Provision Account
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Total Accounts" value={totalCount} tone="amber" icon={UsersIcon} />
        <StatCard title="Administrators" value={adminCount} tone="red" />
        <StatCard title="Teachers" value={teacherCount} tone="sky" />
        <StatCard title="Students" value={studentCount} tone="emerald" />
      </div>

      <Card className="mt-8 space-y-5 p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <div className="w-full max-w-md">
            <TextField
              label="Search Users"
              placeholder="Search by name or email…"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="w-full max-w-xs">
            <SelectField
              label="Filter by Role"
              value={role}
              onChange={(e) => {
                setRole(e.target.value);
                setPage(1);
              }}
            >
              <option value="">All Roles</option>
              {Object.values(UserRole).map((value) => (
                <option key={value} value={value}>
                  {roleLabels[value]}
                </option>
              ))}
            </SelectField>
          </div>
        </div>

        <SortableDataTable
          columns={columns}
          rows={rows}
          loading={data === undefined}
          emptyTitle="No users found"
          emptyHint="Try a different search term, or provision the first account."
          emptyIcon={UsersIcon}
        />

        <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <CreatePanel
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Provision New User"
        description="The account becomes active immediately and can sign in with the password you set."
        fields={USER_FIELDS}
        submitLabel="Provision Account"
        onSubmit={createUser.mutateAsync}
      />

      <ConfirmDialog
        open={pendingDeactivate !== null}
        onClose={() => setPendingDeactivate(null)}
        onConfirm={() => {
          if (pendingDeactivate) {
            void deactivate.mutateAsync(pendingDeactivate);
          }
        }}
        title="Deactivate user"
        message={`Deactivate ${pendingDeactivate?.fullName ?? "this user"}? They will no longer be able to sign in.`}
        confirmLabel="Deactivate"
      />
    </div>
  );
}