// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useSyncExternalStore } from "react";

/**
 * Live prefers-reduced-motion flag.
 *
 * useSyncExternalStore is the correct shape for this: the media query is an external
 * system, and re-renders happen only when the answer actually changes. No effects, no
 * cascading renders. The server snapshot is false; the first client render corrects it.
 */

function subscribe(callback: () => void): () => void {
  const mql = window.matchMedia("(prefers-reduced-motion: reduce)");
  mql.addEventListener("change", callback);

  return () => {
    mql.removeEventListener("change", callback);
  };
}

function getSnapshot(): boolean {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function getServerSnapshot(): boolean {
  return false;
}

export function usePrefersReducedMotion(): boolean {
  return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
}