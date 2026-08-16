// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  setSession,
} from "./tokens";
import type { AuthResponse } from "./types";

/**
 * The single door to the API.
 *
 * Everything the UI knows about HTTP lives here: the bearer header, RFC 7807 error
 * parsing, and one silent refresh-and-retry when an access token has expired. Screens
 * call get/post/put/del and handle ApiError, never Response.
 */

const BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080/api/v1";

/**
 * A failed request, already decoded.
 *
 * The API answers errors as application/problem+json with a title, a detail, and — for
 * 422 — a per-field errors map, so a form can put messages back on the fields that
 * caused them instead of showing one banner for everything.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
  readonly traceId?: string;

  constructor(
    status: number,
    title: string,
    detail?: string,
    errors?: Record<string, string[]>,
    traceId?: string,
  ) {
    super(detail?.trim() || title);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.errors = errors;
    this.traceId = traceId;
  }

  /** Field-level messages for react-hook-form, flattened to one per field. */
  get fieldErrors(): Record<string, string> {
    const flattened: Record<string, string> = {};

    for (const [field, messages] of Object.entries(this.errors ?? {})) {
      if (messages.length > 0) {
        // camelCase the server's PascalCase property names so they match form field ids.
        flattened[field.charAt(0).toLowerCase() + field.slice(1)] = messages[0];
      }
    }

    return flattened;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  traceId?: string;
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};

  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // A body that is not JSON (a proxy error page, an empty 502) still has to surface as
    // something a user can read.
  }

  return new ApiError(
    response.status,
    problem.title ?? response.statusText ?? "Request failed",
    problem.detail,
    problem.errors,
    problem.traceId,
  );
}

/**
 * Exchanges the refresh token for a new pair.
 *
 * Shared between concurrent callers: several requests can 401 at once when a token
 * expires, and without this they would each spend the same refresh token. The API rotates
 * refresh tokens and treats a replayed one as a breach — it revokes the whole chain — so
 * racing here would log the user out rather than keep them signed in.
 */
let inFlightRefresh: Promise<boolean> | null = null;

function refreshSession(): Promise<boolean> {
  inFlightRefresh ??= (async () => {
    try {
      const token = getRefreshToken();

      if (!token) {
        return false;
      }

      const response = await fetch(`${BASE_URL}/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: token }),
      });

      if (!response.ok) {
        return false;
      }

      setSession((await response.json()) as AuthResponse);
      return true;
    } catch {
      return false;
    } finally {
      inFlightRefresh = null;
    }
  })();

  return inFlightRefresh;
}

interface RequestOptions {
  method: string;
  path: string;
  body?: unknown;
  /** Set on the login and refresh calls, which must not try to refresh on failure. */
  anonymous?: boolean;
}

async function send<T>(options: RequestOptions): Promise<T> {
  const { method, path, body, anonymous } = options;

  const call = async (): Promise<Response> => {
    const headers: Record<string, string> = {};

    if (body !== undefined) {
      headers["Content-Type"] = "application/json";
    }

    const token = anonymous ? null : getAccessToken();

    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  };

  let response = await call();

  // One retry, and only for an expired token. A second 401 means the refresh token is
  // gone too, so the session is genuinely over.
  if (response.status === 401 && !anonymous) {
    if (await refreshSession()) {
      response = await call();
    }

    if (response.status === 401) {
      clearSession();

      if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
        // A full document navigation, not a router push, and deliberately so: the session
        // is gone, and reloading is the surest way to discard every piece of in-memory
        // state belonging to the previous user. This module also sits outside the React
        // tree, where no router is available to call.
        // eslint-disable-next-line @next/next/no-location-assign-relative-destination
        window.location.href = "/login?expired=1";
      }

      throw await toApiError(response);
    }
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// ---------------------------------------------------------------------------------

export const api = {
  get: <T>(path: string) => send<T>({ method: "GET", path }),
  post: <T>(path: string, body?: unknown) => send<T>({ method: "POST", path, body }),
  put: <T>(path: string, body?: unknown) => send<T>({ method: "PUT", path, body }),
  del: <T = void>(path: string) => send<T>({ method: "DELETE", path }),

  /**
   * login: email + password plus the honeypot field. The honeypot is an invisible form
   * field a human never fills; a bot that autofills it gets a 422 from the API before any
   * credential work happens. It is sent with the request so the trap is server-side.
   */
  login: (email: string, password: string, honeypot?: string) =>
    send<AuthResponse>({
      method: "POST",
      path: "/auth/login",
      body: { email, password, honeypot: honeypot ?? "" },
      anonymous: true,
    }),
};

/**
 * Builds a query string from only the parameters that carry a value.
 *
 * The API rejects unknown query keys with 400 rather than ignoring them, so a caller must
 * not send a filter belonging to a different route — and an undefined value must be left
 * out entirely rather than sent as the string "undefined".
 */
export function query(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const encoded = search.toString();

  return encoded ? `?${encoded}` : "";
}
