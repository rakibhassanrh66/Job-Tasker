// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/lib/auth-context";
import { roleHome, type UserRole } from "@/lib/types";
import { Spinner } from "./ui";

/**
 * Keeps a role out of another role's screens.
 *
 * This is convenience, not security. Every endpoint behind these screens enforces its own
 * role gate server-side — see AuthorizationMatrixTests, which asserts exactly that for all
 * 41 routes — so a user who defeats this guard reaches pages that then answer 403 to every
 * request they make. Its job is to send people somewhere useful instead of showing them a
 * screen full of errors.
 */
export function RequireRole({
  roles,
  children,
}: {
  roles: readonly UserRole[];
  children: React.ReactNode;
}) {
  const { user, ready } = useAuth();
  const router = useRouter();

  const permitted = user !== null && roles.includes(user.role);

  useEffect(() => {
    if (!ready) {
      return;
    }

    if (!user) {
      router.replace("/login");
      return;
    }

    if (!permitted) {
      // Their own home rather than /login: they are signed in, just in the wrong place.
      router.replace(roleHome[user.role]);
    }
  }, [ready, user, permitted, router]);

  if (!ready || !permitted) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner label="Checking your session" />
      </div>
    );
  }

  return <>{children}</>;
}
