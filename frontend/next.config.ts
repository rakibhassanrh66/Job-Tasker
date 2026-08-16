// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import type { NextConfig } from "next";

const apiOrigin =
  new URL(process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080/api/v1").origin;

/**
 * Strict-but-practical content security policy.
 *
 * script-src keeps 'unsafe-inline' because the Next App Router injects inline scripts for
 * hydration; style-src keeps it because React writes some styles inline. Everything else
 * is locked down: no object/embed, no frames (also enforced by X-Frame-Options), and
 * connect-src allows only this site, the API origin, and websockets (dev HMR / API).
 */
const csp = [
  "default-src 'self'",
  `connect-src 'self' ${apiOrigin} ws: wss:`,
  "img-src 'self' data: blob:",
  "font-src 'self' data:",
  "script-src 'self' 'unsafe-inline'",
  "style-src 'self' 'unsafe-inline'",
  "object-src 'none'",
  "frame-ancestors 'none'",
  "base-uri 'self'",
  "form-action 'self'",
].join("; ");

const securityHeaders = [
  { key: "Content-Security-Policy", value: csp },
  // Only meaningful over HTTPS — browsers ignore it on plain HTTP — so it is harmless
  // during localhost evaluation and applies the moment the site is served over TLS.
  { key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" },
  { key: "X-Frame-Options", value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
  { key: "X-Permitted-Cross-Domain-Policies", value: "none" },
];

const nextConfig: NextConfig = {
  // Docker sets NEXT_DOCKER=1 so the image can copy .next/standalone and run server.js;
  // on Vercel's hosted build this flag is absent and the default serverless output is used.
  output: process.env.NEXT_DOCKER === "1" ? "standalone" : undefined,
  poweredByHeader: false,

  async headers() {
    return [
      {
        source: "/(.*)",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;
