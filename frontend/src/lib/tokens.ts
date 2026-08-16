// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import type { AuthResponse, UserProfile } from "./types";

/**
 * Where the session lives.
 *
 * The browser calls the API directly — CORS is configured for this origin and
 * NEXT_PUBLIC_API_URL is baked into the bundle — so the access token has to be readable
 * by JavaScript in order to go into an Authorization header. That rules out an httpOnly
 * cookie and means an XSS bug would expose the token; the mitigation is that access
 * tokens live 15 minutes and refresh tokens rotate on every use, so a stolen pair is
 * detectable and short-lived rather than permanent. This is recorded in the README's
 * limitations rather than hidden.
 *
 * Kept out of React state on purpose: the fetch wrapper needs the token during a refresh
 * retry, which can happen while no component is rendering.
 */

const ACCESS_KEY = "asms.accessToken";
const REFRESH_KEY = "asms.refreshToken";
const USER_KEY = "asms.user";

/**
 * Plain, non-httpOnly marker cookie written alongside the tokens.
 *
 * It exists solely so the edge middleware can route a logged-in user away from /login and
 * a logged-out user away from the role dashboards on the first navigation. It carries no
 * secret — the actual token never leaves localStorage — so it is a convenience, not a
 * security boundary. It cannot be HttpOnly because the browser's JavaScript has to create
 * and delete it.
 */
const SESSION_COOKIE = "asms.session";
const SESSION_COOKIE_DAYS = 30;

let accessToken: string | null = null;
let refreshToken: string | null = null;
let user: UserProfile | null = null;

let hydrated = false;

/** Reads localStorage once per page load. Safe to call during render — it no-ops on the
 *  server, where localStorage does not exist. */
function hydrate(): void {
  if (hydrated || typeof window === "undefined") {
    return;
  }

  hydrated = true;
  accessToken = window.localStorage.getItem(ACCESS_KEY);
  refreshToken = window.localStorage.getItem(REFRESH_KEY);

  const raw = window.localStorage.getItem(USER_KEY);

  if (raw) {
    try {
      user = JSON.parse(raw) as UserProfile;
    } catch {
      // A corrupt entry is not worth crashing the app over — treat it as logged out.
      user = null;
    }
  }
}

/**
 * Subscription plumbing so React can read the session through useSyncExternalStore.
 *
 * The session lives outside React — the fetch wrapper writes to it during a refresh, with
 * no component involved — which makes it an external store in exactly the sense that hook
 * exists for. Reading it that way instead of copying it into state with an effect avoids
 * the cascading render that a setState-in-effect causes.
 */
const listeners = new Set<() => void>();

export interface Session {
  user: UserProfile | null;

  /**
   * Whether this snapshot reflects stored state.
   *
   * False only while rendering on the server and during hydration, where localStorage does
   * not exist and every session therefore looks empty. Guards wait for it, so a signed-in
   * user is not bounced to /login on every refresh.
   *
   * It comes from the store rather than an effect precisely because
   * useSyncExternalStore already distinguishes the two: it renders the server snapshot
   * through hydration, then switches to the client one. That switch *is* readiness, so
   * tracking it separately would be duplicating what the hook already knows.
   */
  ready: boolean;
}

/** Constant identity. useSyncExternalStore compares snapshots by reference, so a fresh
 *  object per read would re-render forever. */
const SERVER_SESSION: Session = { user: null, ready: false };

let snapshot: Session | null = null;

function publish(): void {
  for (const listener of listeners) {
    listener();
  }
}

export function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getSessionSnapshot(): Session {
  snapshot ??= { user: getUser(), ready: true };
  return snapshot;
}

export function getServerSessionSnapshot(): Session {
  return SERVER_SESSION;
}

export function getAccessToken(): string | null {
  hydrate();
  return accessToken;
}

export function getRefreshToken(): string | null {
  hydrate();
  return refreshToken;
}

export function getUser(): UserProfile | null {
  hydrate();
  return user;
}

export function setSession(auth: AuthResponse): void {
  hydrated = true;
  accessToken = auth.accessToken;
  refreshToken = auth.refreshToken;
  user = auth.user;

  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(ACCESS_KEY, auth.accessToken);
  window.localStorage.setItem(REFRESH_KEY, auth.refreshToken);
  window.localStorage.setItem(USER_KEY, JSON.stringify(auth.user));

  setSessionCookie();

  snapshot = { user: auth.user, ready: true };
  publish();
}

export function clearSession(): void {
  hydrated = true;
  accessToken = null;
  refreshToken = null;
  user = null;

  snapshot = { user: null, ready: true };

  if (typeof window !== "undefined") {
    window.localStorage.removeItem(ACCESS_KEY);
    window.localStorage.removeItem(REFRESH_KEY);
    window.localStorage.removeItem(USER_KEY);
    clearSessionCookie();
  }

  publish();
}

function setSessionCookie(): void {
  if (typeof window === "undefined") {
    return;
  }

  const expires = new Date(Date.now() + SESSION_COOKIE_DAYS * 24 * 60 * 60 * 1000).toUTCString();
  document.cookie = `${SESSION_COOKIE}=1; path=/; samesite=lax; expires=${expires}`;
}

function clearSessionCookie(): void {
  if (typeof window === "undefined") {
    return;
  }

  document.cookie = `${SESSION_COOKIE}=; path=/; samesite=lax; max-age=0`;
}
