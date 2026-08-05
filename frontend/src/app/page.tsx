// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { Spinner } from "@/components/ui";
import { useAuth } from "@/lib/auth-context";
import { roleHome } from "@/lib/types";

/** The root has no content of its own — it decides where the caller belongs and sends
 *  them there. */
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
    <div className="flex min-h-screen items-center justify-center">
      <Spinner label="Loading" />
    </div>
  );
}
