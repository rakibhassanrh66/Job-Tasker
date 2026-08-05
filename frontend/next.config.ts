// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Required by frontend/Dockerfile, which copies .next/standalone and runs server.js.
  // Without this the image builds and then fails to start.
  output: "standalone",
};

export default nextConfig;
