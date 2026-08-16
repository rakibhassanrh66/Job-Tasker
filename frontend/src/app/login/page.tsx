// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, KeyRound, ShieldCheck, UserRound } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button, ErrorBanner, TextField } from "@/components/ui";
import { useAuth } from "@/lib/auth-context";
import { useToast } from "@/lib/toast";
import { labelFor, roleHome, type UserRole } from "@/lib/types";

const schema = z.object({
  email: z.string().min(1, "Email is required.").email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
  // Honeypot: left invisible and intentionally unvalidated here — the API is the one that
  // rejects a filled value, so bots that bypass client validation still get caught.
  website: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const roleLabels: Record<UserRole, string> = {
  [1]: "Admin",
  [2]: "Teacher",
  [3]: "Student",
};

const DEMO_ACCOUNTS: {
  role: UserRole;
  email: string;
  password: string;
  description: string;
}[] = [
  {
    role: 1,
    email: "admin@demo.test",
    password: "Admin@123",
    description: "Users, classes, subjects, allocations, full oversight",
  },
  {
    role: 2,
    email: "teacher@demo.test",
    password: "Teacher@123",
    description: "Create, publish and grade assignments",
  },
  {
    role: 3,
    email: "student@demo.test",
    password: "Student@123",
    description: "View assignments, submit work, check grades",
  },
];

function LoginForm() {
  const { login, user, ready } = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const [failure, setFailure] = useState<unknown>(null);
  const { success } = useToast();

  const expired = params.get("expired") === "1";

  // Already signed in (fresh token or restored session) → straight to their dashboard.
  useEffect(() => {
    if (ready && user) {
      router.replace(roleHome[user.role]);
    }
  }, [ready, user, router]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null);

    try {
      const profile = await login(values.email, values.password, values.website);
      success("Signed in successfully", `Welcome back, ${profile.fullName}!`);
      router.replace(roleHome[profile.role]);
    } catch (cause) {
      setFailure(cause);
    }
  });

  const handleSelectRole = (account: (typeof DEMO_ACCOUNTS)[number]) => {
    reset({ email: account.email, password: account.password });
    setFailure(null);
  };

  const handleInstantLogin = async (account: (typeof DEMO_ACCOUNTS)[number]) => {
    reset({ email: account.email, password: account.password });
    setFailure(null);

    try {
      const profile = await login(account.email, account.password);
      success("Signed in successfully", `Welcome back, ${profile.fullName}!`);
      router.replace(roleHome[profile.role]);
    } catch (cause) {
      setFailure(cause);
    }
  };

  return (
    <main className="relative mx-auto flex min-h-screen w-full max-w-7xl flex-col items-center justify-center gap-12 px-4 py-16 sm:px-6 lg:flex-row lg:gap-20 lg:py-24">
      {/* Left: editorial hero */}
      <section className="w-full max-w-xl shrink-0 lg:max-w-2xl">
        <p className="text-[11px] font-bold uppercase tracking-[0.3em] text-accent-400">
          OnnoRokom Projukti Ltd · Evaluation Build
        </p>

        <h1 className="mt-5 text-5xl font-extrabold leading-[1.05] tracking-tight text-white sm:text-6xl xl:text-7xl">
          Assignment
          <br />
          Management
          <br />
          <span className="text-slate-500">System.</span>
        </h1>

        <p className="mt-6 max-w-lg text-base leading-relaxed text-slate-400 sm:text-lg">
          A secure academic submission platform. Roles are separated at the API level; every
          screen behind this door answers only to the endpoints its role may call.
        </p>

        <div className="mt-10 flex flex-wrap items-center gap-2.5">
          {[
            { icon: ShieldCheck, label: "JWT Auth" },
            { icon: UserRound, label: "3 Roles" },
            { icon: KeyRound, label: "Role Gates" },
          ].map((item) => (
            <span
              key={item.label}
              className="inline-flex items-center gap-2 rounded-md border border-line bg-ink-900/70 px-3 py-1.5 font-mono text-xs text-slate-400"
            >
              <item.icon className="h-3.5 w-3.5 text-accent-400" aria-hidden />
              {item.label}
            </span>
          ))}
        </div>
      </section>

      {/* Right: credential panel */}
      <section className="w-full max-w-md">
        <div className="rounded-md border border-line-strong bg-ink-900/80 p-8 shadow-2xl shadow-black/40 backdrop-blur-sm">
          <p className="text-[11px] font-bold uppercase tracking-[0.25em] text-slate-500">
            Sign in
          </p>
          <h2 className="mt-2 text-2xl font-extrabold tracking-tight text-white">
            Authenticate
          </h2>
          <p className="mt-1 text-sm text-slate-400">
            Pick a role to load its evaluation credentials, then authenticate.
          </p>

          {/* Role selector — prefills the seeded demo credentials */}
          <div className="mt-6 grid gap-2">
            {DEMO_ACCOUNTS.map((account) => (
              <div
                key={account.role}
                className="flex items-center gap-3 rounded-md border border-line bg-ink-850 px-3.5 py-3 transition-colors duration-200 hover:border-ink-500"
              >
                <button
                  type="button"
                  onClick={() => handleSelectRole(account)}
                  className="flex flex-1 cursor-pointer items-center gap-3 text-left"
                >
                  <span
                    className={`h-2 w-2 shrink-0 rounded-full ${
                      account.role === 1
                        ? "bg-red-400"
                        : account.role === 2
                          ? "bg-accent-400"
                          : "bg-sky-400"
                    }`}
                    aria-hidden
                  />
                  <span className="min-w-0">
                    <span className="block text-sm font-bold text-slate-100">
                      {labelFor(roleLabels, account.role)}
                    </span>
                    <span className="block truncate font-mono text-[11px] text-slate-500">
                      {account.email}
                    </span>
                  </span>
                </button>

                <button
                  type="button"
                  onClick={() => handleInstantLogin(account)}
                  title="Sign in instantly as this role"
                  aria-label={`Sign in instantly as ${labelFor(roleLabels, account.role)}`}
                  className="inline-flex shrink-0 cursor-pointer items-center gap-1 rounded-md px-2 py-1 text-[11px] font-bold uppercase tracking-wider text-accent-400 transition-colors duration-150 hover:bg-ink-800 hover:text-accent-300"
                >
                  Enter
                  <ArrowRight className="h-3 w-3" aria-hidden />
                </button>
              </div>
            ))}
          </div>

          <form onSubmit={onSubmit} className="mt-6 space-y-4" noValidate>
            {/* Honeypot field. Invisible to humans, tempting to bots: a filled value is
                rejected by the API before any credential work takes place. */}
            <div className="absolute -left-[9999px] h-0 w-0 overflow-hidden" aria-hidden="true">
              <label htmlFor="website">Website</label>
              <input
                id="website"
                type="text"
                tabIndex={-1}
                autoComplete="off"
                {...register("website")}
              />
            </div>

            {expired && (
              <p className="rounded-md border border-accent-400/40 bg-amber-950/40 p-3 text-sm text-amber-200">
                Your session expired. Please sign in again.
              </p>
            )}

            <ErrorBanner error={failure} />

            <TextField
              label="Email Address"
              type="email"
              autoComplete="username"
              placeholder="name@demo.test"
              error={errors.email?.message}
              {...register("email")}
            />

            <TextField
              label="Password"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              error={errors.password?.message}
              {...register("password")}
            />

            <Button
              type="submit"
              size="lg"
              className="w-full"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Authenticating…" : "Authenticate"}
              {!isSubmitting && <ArrowRight className="h-4 w-4" aria-hidden />}
            </Button>
          </form>
        </div>

        <p className="mt-4 text-center font-mono text-[11px] text-slate-500">
          seeded accounts · admin@demo.test / teacher@demo.test / student@demo.test
        </p>
      </section>
    </main>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={<div className="flex min-h-screen items-center justify-center">Loading…</div>}>
      <LoginForm />
    </Suspense>
  );
}