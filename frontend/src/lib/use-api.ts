// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useCallback, useEffect, useState } from "react";

interface State<T> {
  data: T | null;
  error: unknown;
  loading: boolean;
}

/**
 * Load-on-mount with a manual reload, which is the shape every screen here needs: fetch a
 * list, then refetch after a create, edit or delete.
 *
 * `deps` are the values the fetcher closes over — a page number, a filter. Changing one
 * refetches. The fetcher itself is deliberately not a dependency: screens define it inline,
 * so a new function identity every render would loop forever.
 */
export function useApi<T>(fetcher: () => Promise<T>, deps: readonly unknown[] = []) {
  const [nonce, setNonce] = useState(0);
  const key = `${JSON.stringify(deps)}#${nonce}`;

  const [state, setState] = useState<State<T>>({ data: null, error: null, loading: true });
  const [renderedKey, setRenderedKey] = useState(key);

  // Reset while rendering rather than inside the effect. React supports adjusting state
  // when an input changes, and doing it here keeps the effect body free of synchronous
  // setState — which would render once with stale data before the fetch had even started.
  if (key !== renderedKey) {
    setRenderedKey(key);
    setState({ data: null, error: null, loading: true });
  }

  const reload = useCallback(() => setNonce((n) => n + 1), []);

  useEffect(() => {
    let cancelled = false;

    fetcher()
      .then((data) => {
        // A slow earlier request must not overwrite the result of a faster later one.
        if (!cancelled) {
          setState({ data, error: null, loading: false });
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setState({ data: null, error, loading: false });
        }
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  return { ...state, reload };
}
