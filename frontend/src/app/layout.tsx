// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import type { Metadata } from "next";
import { AuthProvider } from "@/lib/auth-context";
import "./globals.css";

// No next/font/google here on purpose. It fetches the font files during `next build`,
// which makes the Docker image build depend on outbound network access to Google — a
// build that fails on a machine behind a proxy, for a typeface. The system stack below
// costs nothing and renders everywhere.

export const metadata: Metadata = {
  title: "Assignment & Submission System",
  description:
    "Role-based assignment and submission management for schools and colleges. "
    + "Evaluation build by Rakib Hassan.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="flex min-h-full flex-col bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
