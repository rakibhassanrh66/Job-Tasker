// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { ShieldCheck } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/lib/auth-context";
import { roleHome } from "@/lib/types";

/** The root has no content of its own — it decides where the caller belongs and sends
 *  them there. A centered brand mark keeps the moment readable while the token check runs. */
export default function HomePage() {
  const { user, ready } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!ready) {
      return;
    }

    router.replace(user ? roleHome[user.role] : "/login");
  }, [ready, user, router]);

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <span className="flex h-14 w-14 items-center justify-center rounded-md bg-accent-400 text-ink-950 animate-pulse">
        <ShieldCheck className="h-7 w-7" aria-hidden />
      </span>
      <p className="font-mono text-xs uppercase tracking-[0.25em] text-slate-500">
        Assignment MS · routing you
      </p>
    </div>
  );
}