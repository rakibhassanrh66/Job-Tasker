// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button, Card, ErrorBanner, TextField } from "@/components/ui";
import { useAuth } from "@/lib/auth-context";
import { roleHome } from "@/lib/types";

// Mirrors the server's LoginRequestValidator. Catching an empty field here saves a round
// trip; the server still validates, because a client check is a convenience and never a
// guarantee.
const schema = z.object({
  email: z.string().min(1, "Email is required.").email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
});

type FormValues = z.infer<typeof schema>;

function LoginForm() {
  const { login, user, ready } = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const [failure, setFailure] = useState<unknown>(null);

  const expired = params.get("expired") === "1";

  // Someone already signed in has no business on the login page.
  useEffect(() => {
    if (ready && user) {
      router.replace(roleHome[user.role]);
    }
  }, [ready, user, router]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);

    try {
      const profile = await login(values.email, values.password);
      router.replace(roleHome[profile.role]);
    } catch (cause) {
      setFailure(cause);
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center px-4 py-12">
      <div className="w-full max-w-md space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-semibold tracking-tight">Assignment System</h1>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Sign in to continue
          </p>
        </div>

        <Card>
          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            {expired && (
              <p className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
                Your session expired. Please sign in again.
              </p>
            )}

            <ErrorBanner error={failure} />

            <TextField
              label="Email"
              type="email"
              autoComplete="username"
              placeholder="you@example.com"
              error={errors.email?.message}
              {...register("email")}
            />

            <TextField
              label="Password"
              type="password"
              autoComplete="current-password"
              error={errors.password?.message}
              {...register("password")}
            />

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Signing in…" : "Sign in"}
            </Button>
          </form>
        </Card>

        <Card className="text-sm">
          <p className="font-medium text-slate-700 dark:text-slate-200">Demo accounts</p>
          <p className="mt-1 text-xs text-slate-500">
            Seeded, throwaway evaluation accounts — these are documented in the README and
            are not secrets.
          </p>
          <dl className="mt-3 space-y-1.5 font-mono text-xs text-slate-600 dark:text-slate-400">
            <div className="flex justify-between gap-3">
              <dt>admin@demo.test</dt>
              <dd>Admin@123</dd>
            </div>
            <div className="flex justify-between gap-3">
              <dt>teacher@demo.test</dt>
              <dd>Teacher@123</dd>
            </div>
            <div className="flex justify-between gap-3">
              <dt>student@demo.test</dt>
              <dd>Student@123</dd>
            </div>
          </dl>
        </Card>
      </div>
    </div>
  );
}

export default function LoginPage() {
  // useSearchParams needs a Suspense boundary, or the whole route opts out of static
  // rendering at build time.
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
