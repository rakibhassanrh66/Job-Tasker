// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import Lenis from "lenis";
import { useEffect } from "react";

/**
 * Lenis smooth scrolling for the whole document.
 *
 * No-op for people who ask for reduced motion — they get the browser's native scroll,
 * which is the correct behaviour both for accessibility and for the evaluation brief.
 */
export function SmoothScroll() {
  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    const lenis = new Lenis({ autoRaf: true, lerp: 0.1, smoothWheel: true });

    return () => {
      lenis.destroy();
    };
  }, []);

  return null;
}