// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from "react";
import { api } from "./api";
import {
  clearSession,
  getServerSessionSnapshot,
  getSessionSnapshot,
  setSession,
  subscribe,
} from "./tokens";
import type { UserProfile } from "./types";

interface AuthState {
  user: UserProfile | null;
  /** False until hydration has run — see Session.ready. Guards wait for it rather than
   *  redirecting on the first render. */
  ready: boolean;
  login: (email: string, password: string, honeypot?: string) => Promise<UserProfile>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // The session is written from outside React — the fetch wrapper replaces it during a
  // token refresh, with no component involved — so it is read as an external store rather
  // than mirrored into state with an effect. No effects in this provider at all.
  const session = useSyncExternalStore(subscribe, getSessionSnapshot, getServerSessionSnapshot);

  const login = useCallback(async (email: string, password: string, honeypot?: string) => {
    const auth = await api.login(email, password, honeypot);

    // Writing to the store notifies every subscriber, this provider included.
    setSession(auth);

    return auth.user;
  }, []);

  const logout = useCallback(() => clearSession(), []);

  const value = useMemo<AuthState>(
    () => ({ user: session.user, ready: session.ready, login, logout }),
    [session, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside an AuthProvider.");
  }

  return context;
}
