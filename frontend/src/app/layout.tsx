// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { CursorField } from "@/components/effects/CursorField";
import { PageTransition } from "@/components/layout/PageTransition";
import { SmoothScroll } from "@/components/smooth-scroll";
import { AuthProvider } from "@/lib/auth-context";
import { QueryProvider } from "@/lib/query";
import { ToastProvider } from "@/lib/toast";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Assignment & Submission Management System",
  description:
    "Role-based assignment and submission management for schools and colleges. "
    + "Evaluation build by Rakib Hassan for OnnoRokom Projukti Ltd.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={`${inter.variable} h-full antialiased`}>
      <body className="relative flex min-h-full flex-col bg-ink-950 text-slate-300">
        {/* Full-viewport mouse-reactive canvas grid, behind everything */}
        <CursorField />

        <AuthProvider>
          <QueryProvider>
            <ToastProvider>
              {/* Exit/enter transition on every route change */}
              <PageTransition>{children}</PageTransition>
            </ToastProvider>
          </QueryProvider>
        </AuthProvider>

        {/* Lenis smooth scrolling; no-ops under prefers-reduced-motion */}
        <SmoothScroll />
      </body>
    </html>
  );
}