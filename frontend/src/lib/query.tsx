// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import {
  QueryClient,
  QueryClientProvider,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryKey,
  type UseMutationOptions,
} from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { ApiError } from "@/lib/api";
import { useToast } from "@/lib/toast";
import type { PagedResult } from "@/lib/types";

/**
 * TanStack Query wiring. Every screen reads server state through here:
 *
 * - `useApiQuery` — one-shot fetches (assignment detail, allocations list)
 * - `useApiPagedQuery` — list screens with page/search/status/role filters
 * - `useApiMutation` — writes; shows a success/error toast, invalidates the keys the
 *   screen depends on, and surfaces field-level 422 messages to the caller
 *
 * Query keys are built from the same query string the API validates, so a filter change
 * is a key change and cache entries never collide across screens.
 */

export function makeQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Lists change only when someone writes; 30s of freshness keeps navigation
        // instant without staleness ever mattering.
        staleTime: 30_000,
        retry: 1,
        refetchOnWindowFocus: false,
      },
      mutations: {
        // A write is either accepted or refused by the API; retrying is pointless.
        retry: 0,
      },
    },
  });
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  // One client per app lifetime. React Query re-renders are driven by its own store.
  const [client] = useState(makeQueryClient);

  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

// ---------------------------------------------------------------------------------
// Query keys
// ---------------------------------------------------------------------------------

export const queryKeys = {
  users: ["users"] as const,
  classes: ["classes"] as const,
  subjects: ["subjects"] as const,
  allocations: ["teacher-assignments"] as const,
  enrolments: ["enrollments"] as const,
  assignments: ["assignments"] as const,
  submissions: ["submissions"] as const,
};

export interface ListFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  role?: string;
  teacherId?: string;
  classCourseId?: string;
}

function filterParts(filters: ListFilters): (string | number)[] {
  return Object.entries(filters)
    .filter(([, value]) => value !== undefined && value !== "")
    .flatMap(([key, value]) => [key, value as string | number]);
}

// ---------------------------------------------------------------------------------
// Reads
// ---------------------------------------------------------------------------------

export function useApiQuery<T>(
  key: QueryKey,
  fetcher: () => Promise<T>,
  enabled = true,
) {
  return useQuery({
    queryKey: key,
    queryFn: fetcher,
    enabled,
    // The API already answers with a coherent 404 for "not found"; a single retry
    // covers transient network blips without hammering a missing resource.
    retry: (failureCount, error) =>
      error instanceof ApiError && error.status >= 400 && error.status < 500
        ? false
        : failureCount < 1,
  });
}

export function useApiPagedQuery<T>(
  base: readonly unknown[],
  filters: ListFilters,
  fetcher: () => Promise<PagedResult<T>>,
) {
  const parts = filterParts(filters);
  const key = [...base, "page", ...parts];

  // The fetcher is recreated per render; the query key owns cache identity, and
  // TanStack Query dedupes by key, not by function identity.
  return useApiQuery(key, fetcher);
}

// ---------------------------------------------------------------------------------
// Writes
// ---------------------------------------------------------------------------------

export function useApiMutation<TData, TVariables = void>(options: {
  mutationFn: (variables: TVariables) => Promise<TData>;
  /** Query keys to invalidate after success. */
  invalidate?: QueryKey[];
  /** Toast title/body on success. Defaults to none. */
  successMessage?: string | ((data: TData, variables: TVariables) => string);
  onSuccess?: (data: TData, variables: TVariables) => void;
  /** Called with the raw error so a screen can map 422 field messages. */
  onError?: (error: unknown, variables: TVariables) => void;
} & Omit<
  UseMutationOptions<TData, unknown, TVariables>,
  "mutationFn" | "onSuccess" | "onError"
>) {
  const toast = useToast();
  const queryClient = useQueryClient();

  return useMutation<TData, unknown, TVariables>({
    ...options,
    mutationFn: options.mutationFn,
    onSuccess: (data, variables) => {
      const message =
        typeof options.successMessage === "function"
          ? options.successMessage(data, variables)
          : options.successMessage;

      if (message) {
        toast.success(message);
      }

      for (const key of options.invalidate ?? []) {
        void queryClient.invalidateQueries({ queryKey: key });
      }

      options.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      if (error instanceof ApiError) {
        toast.error(error.title, error.detail);
      } else if (error instanceof Error) {
        toast.error("Request failed", error.message);
      } else {
        toast.error("Request failed");
      }

      options.onError?.(error, variables);
    },
  });
}

/** Small helper for the rare screen that mutates without a shared mutation instance. */
export function useInvalidate() {
  const queryClient = useQueryClient();
  return useCallback((keys: QueryKey[]) => {
    for (const key of keys) {
      void queryClient.invalidateQueries({ queryKey: key });
    }
  }, [queryClient]);
}