// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import { NextRequest, NextResponse } from "next/server";

/**
 * Edge-level navigation guard.
 *
 * The real authorization boundary is the API: every request is checked server-side there,
 * and the access token lives in localStorage (a non-httpOnly cookie cannot be read by the
 * edge either, since the token must reach the browser's JavaScript). This middleware
 * therefore does not — and cannot — enforce security. It only smooths navigation using a
 * plain session-marker cookie (see lib/tokens.ts): keep logged-out visitors off the role
 * dashboards and keep logged-in visitors off the login screen, so the client-side guards
 * are not the first thing a user sees.
 *
 * Security headers are applied in next.config.ts; they must not be duplicated here.
 */

const PROTECTED_PREFIXES = ["/admin", "/teacher", "/student"];
const SESSION_COOKIE = "asms.session";

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasSession = request.cookies.has(SESSION_COOKIE);

  const isProtected = PROTECTED_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );

  if (isProtected && !hasSession) {
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    url.search = "";
    return NextResponse.redirect(url);
  }

  if (pathname === "/login" && hasSession) {
    // The root page resolves the role and sends the user to their dashboard.
    const url = request.nextUrl.clone();
    url.pathname = "/";
    url.search = "";
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/admin/:path*", "/teacher/:path*", "/student/:path*", "/login"],
};
