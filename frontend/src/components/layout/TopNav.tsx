// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, motion, type Variants } from "motion/react";
import { ChevronDown, LogOut, Menu, Settings, UserRound, X } from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth-context";
import { labelFor, roleHome, type UserRole } from "@/lib/types";

/**
 * TopNav — the universal navigation bar shown on every page inside the app shell.
 *
 * Left: brand mark. Center: role-aware links (hidden on mobile). Right: avatar menu with
 * Profile / Settings / Logout. Mobile: hamburger opens a full-screen overlay menu with
 * large tap targets. Logout clears the TanStack Query cache, wipes the session (tokens +
 * marker cookie) and replaces the route so the back button cannot return to a protected
 * page; the edge middleware then redirects any stale navigation to /login.
 */

const roleLabels: Record<UserRole, string> = {
  [1]: "Admin",
  [2]: "Teacher",
  [3]: "Student",
};

const navLinks: Record<UserRole, { href: string; label: string }[]> = {
  [1]: [
    { href: "/admin", label: "Dashboard" },
    { href: "/admin/users", label: "Users" },
    { href: "/admin/classes", label: "Classes" },
    { href: "/admin/subjects", label: "Subjects" },
    { href: "/admin/allocations", label: "Allocations" },
    { href: "/admin/assignments", label: "Assignments" },
    { href: "/admin/submissions", label: "Submissions" },
  ],
  [2]: [
    { href: "/teacher", label: "Dashboard" },
    { href: "/teacher/assignments", label: "My Assignments" },
    { href: "/teacher/assignments/new", label: "New Assignment" },
    { href: "/teacher/assignments", label: "Submissions" },
  ],
  [3]: [
    { href: "/student", label: "Dashboard" },
    { href: "/student/assignments", label: "My Assignments" },
    { href: "/student/submissions", label: "My Submissions" },
  ],
};

const INITIALS_FALLBACK = "U";

/** Staggered reveal for the mobile overlay's link list (stagger 0.05s, rise 0.3s). */
const overlayListVariants: Variants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.05 },
  },
};

const overlayItemVariants: Variants = {
  hidden: { opacity: 0, y: 16 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.3, ease: [0.22, 1, 0.36, 1] },
  },
};

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return INITIALS_FALLBACK;
  }

  return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : "")).toUpperCase();
}

function isActive(pathname: string, href: string): boolean {
  if (href === "/admin" || href === "/teacher" || href === "/student") {
    return pathname === href;
  }

  return pathname.startsWith(href);
}

function useLogout() {
  const { logout } = useAuth();
  const queryClient = useQueryClient();
  const router = useRouter();

  return () => {
    // 1. Drop every cached query so nothing from the previous session can surface later.
    queryClient.clear();

    // 2. Wipe tokens, the stored profile and the session-marker cookie.
    logout();

    // 3. replace(), not push(): the protected page is removed from history, so the back
    // button cannot return to it. The middleware guards any residual navigation anyway.
    router.replace("/login");
  };
}

export function TopNav() {
  const { user, ready } = useAuth();
  const pathname = usePathname();
  const handleLogout = useLogout();

  const [menuOpen, setMenuOpen] = useState(false);
  const [overlayOpen, setOverlayOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const links = user ? navLinks[user.role] : [];
  const initials = user ? initialsOf(user.fullName) : INITIALS_FALLBACK;

  /** Any navigation via the nav's own links dismisses both menus. */
  const closeMenus = () => {
    setMenuOpen(false);
    setOverlayOpen(false);
  };

  // Close the dropdown on outside click and on Escape.
  useEffect(() => {
    if (!menuOpen) {
      return;
    }

    const onPointerDown = (event: PointerEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMenuOpen(false);
      }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [menuOpen]);

  // Lock body scroll while the full-screen overlay is open.
  useEffect(() => {
    document.body.style.overflow = overlayOpen ? "hidden" : "";

    return () => {
      document.body.style.overflow = "";
    };
  }, [overlayOpen]);

  return (
    <>
      <header className="sticky top-0 z-30 h-16 border-b border-line bg-ink-950/80 backdrop-blur-md">
        <div className="mx-auto flex h-full w-full max-w-7xl items-center gap-4 px-4 sm:px-6 lg:px-8">
          {/* Left: brand */}
          <Link
            href={user ? roleHome[user.role] : "/login"}
            className="flex h-full shrink-0 items-center gap-2 text-base font-bold tracking-wider text-slate-100 transition-colors duration-200 hover:text-white"
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-md bg-slate-100 text-sm font-extrabold tracking-tight text-slate-950">
              A
            </span>
            <span className="hidden sm:inline">AMS</span>
          </Link>

          {/* Center: role-aware links (desktop) */}
          {ready && user && (
            <nav className="mx-auto hidden h-full items-center gap-1 md:flex" aria-label="Primary">
              {links.map((link) => {
                const active = isActive(pathname, link.href);

                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    onClick={closeMenus}
                    aria-current={active ? "page" : undefined}
                    className={`inline-flex h-full items-center border-b-2 px-3 text-sm font-medium transition-colors duration-200 focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950 ${
                      active
                        ? "border-slate-100 text-slate-100"
                        : "border-transparent text-slate-400 hover:text-slate-200"
                    }`}
                  >
                    {link.label}
                  </Link>
                );
              })}
            </nav>
          )}

          {/* Right: avatar + dropdown, and the mobile hamburger */}
          <div className="ml-auto flex items-center gap-2 md:ml-0">
            <button
              type="button"
              onClick={() => setMenuOpen((open) => !open)}
              aria-haspopup="menu"
              aria-expanded={menuOpen}
              aria-label="Account menu"
              className="inline-flex min-h-[44px] min-w-[44px] cursor-pointer items-center gap-2 rounded-md px-2 text-slate-200 transition-colors duration-200 hover:bg-ink-800 focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950"
            >
              <span className="flex h-9 w-9 items-center justify-center rounded-full border border-line bg-ink-800 text-sm font-bold text-slate-100">
                {initials}
              </span>
              <ChevronDown
                className={`hidden h-4 w-4 text-slate-500 transition-transform duration-200 sm:block ${
                  menuOpen ? "rotate-180" : ""
                }`}
                aria-hidden
              />
            </button>

            <button
              type="button"
              onClick={() => setOverlayOpen(true)}
              aria-label="Open navigation menu"
              className="inline-flex min-h-[44px] min-w-[44px] cursor-pointer items-center justify-center rounded-md text-slate-300 transition-colors duration-200 hover:bg-ink-800 hover:text-white focus-visible:ring-2 focus-visible:ring-slate-500 focus-visible:ring-offset-2 focus-visible:ring-offset-ink-950 md:hidden"
            >
              <Menu className="h-5 w-5" aria-hidden />
            </button>
          </div>

          {/* Avatar dropdown */}
          <AnimatePresence>
            {menuOpen && ready && user && (
              <motion.div
                ref={menuRef}
                role="menu"
                initial={{ opacity: 0, y: 8, scale: 0.98 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: 8, scale: 0.98 }}
                transition={{ duration: 0.18, ease: [0.22, 1, 0.36, 1] }}
                className="absolute right-4 top-16 z-40 w-60 overflow-hidden rounded-md border border-line bg-ink-900 shadow-2xl shadow-black/50 sm:right-6"
              >
                <div className="border-b border-line px-4 py-3">
                  <p className="truncate text-sm font-semibold text-slate-100">{user.fullName}</p>
                  <p className="mt-0.5 truncate font-mono text-[11px] text-slate-500">
                    {user.email}
                  </p>
                </div>

                <div className="p-1.5">
                  <Link
                    href="/profile"
                    role="menuitem"
                    onClick={closeMenus}
                    className="flex min-h-[44px] items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-slate-300 transition-colors duration-150 hover:bg-ink-800 hover:text-white focus-visible:ring-2 focus-visible:ring-slate-500"
                  >
                    <UserRound className="h-4 w-4 text-slate-500" aria-hidden />
                    Profile
                  </Link>
                  <Link
                    href="/profile?tab=settings"
                    role="menuitem"
                    onClick={closeMenus}
                    className="flex min-h-[44px] items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-slate-300 transition-colors duration-150 hover:bg-ink-800 hover:text-white focus-visible:ring-2 focus-visible:ring-slate-500"
                  >
                    <Settings className="h-4 w-4 text-slate-500" aria-hidden />
                    Settings
                  </Link>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={handleLogout}
                    className="flex min-h-[44px] w-full cursor-pointer items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-red-300 transition-colors duration-150 hover:bg-red-500/10 hover:text-red-200 focus-visible:ring-2 focus-visible:ring-slate-500"
                  >
                    <LogOut className="h-4 w-4" aria-hidden />
                    Logout
                  </button>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </header>

      {/* Mobile: full-screen overlay menu */}
      <AnimatePresence>
        {overlayOpen && ready && user && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="fixed inset-0 z-50 flex flex-col bg-ink-950/95 backdrop-blur-md md:hidden"
            role="dialog"
            aria-modal="true"
            aria-label="Navigation"
          >
            <div className="flex h-16 shrink-0 items-center justify-between border-b border-line px-4">
              <span className="text-base font-bold tracking-wider text-slate-100">AMS</span>
              <button
                type="button"
                onClick={() => setOverlayOpen(false)}
                aria-label="Close navigation menu"
                className="inline-flex min-h-[44px] min-w-[44px] cursor-pointer items-center justify-center rounded-md text-slate-300 transition-colors duration-200 hover:bg-ink-800 hover:text-white"
              >
                <X className="h-5 w-5" aria-hidden />
              </button>
            </div>

            <nav className="flex-1 overflow-y-auto px-6 py-8" aria-label="Primary mobile">
              <p className="text-[11px] font-bold uppercase tracking-[0.25em] text-slate-500">
                {labelFor(roleLabels, user.role)}
              </p>

              <motion.ul
                  className="mt-4 space-y-2"
                  variants={overlayListVariants}
                  initial="hidden"
                  animate="visible"
                >
                  {links.map((link) => {
                    const active = isActive(pathname, link.href);

                    return (
                      <li key={link.href}>
                        <motion.div variants={overlayItemVariants}>
                          <Link
                            href={link.href}
                            onClick={closeMenus}
                            aria-current={active ? "page" : undefined}
                            className={`flex min-h-[52px] items-center rounded-md px-4 text-lg font-semibold transition-colors duration-200 ${
                              active
                                ? "bg-ink-800 text-slate-100"
                                : "text-slate-400 hover:bg-ink-850 hover:text-slate-100"
                            }`}
                          >
                            {link.label}
                          </Link>
                        </motion.div>
                      </li>
                    );
                  })}
                </motion.ul>
            </nav>

            <div className="shrink-0 border-t border-line px-6 py-6">
              <button
                type="button"
                onClick={handleLogout}
                className="flex min-h-[52px] w-full cursor-pointer items-center justify-center gap-3 rounded-md border border-red-500/40 bg-red-950/30 text-base font-semibold text-red-300 transition-colors duration-200 hover:bg-red-500/15 hover:text-red-200"
              >
                <LogOut className="h-5 w-5" aria-hidden />
                Logout
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
}