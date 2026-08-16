// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { AnimatePresence, motion } from "motion/react";
import { usePathname } from "next/navigation";
import { usePrefersReducedMotion } from "@/lib/use-prefers-reduced-motion";

/**
 * PageTransition — keyed AnimatePresence wrapper so every route change plays an exit
 * (fade out, drift up) followed by an enter (fade in, drift up) with the system easing.
 *
 * The key is the pathname: a navigation remounts the motion.div, and AnimatePresence
 * runs the old one's exit before the new one's enter. Reduced-motion users get the
 * plain children with no animation.
 */

const EASE: [number, number, number, number] = [0.22, 1, 0.36, 1];

export function PageTransition({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const reduced = usePrefersReducedMotion();

  if (reduced) {
    return <>{children}</>;
  }

  return (
    <AnimatePresence mode="wait" initial={false}>
      <motion.div
        key={pathname}
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -10 }}
        transition={{ duration: 0.3, ease: EASE }}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  );
}