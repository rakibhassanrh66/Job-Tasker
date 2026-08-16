// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

"use client";

import { useEffect, useRef } from "react";

/**
 * CursorField — the mouse-reactive canvas backdrop.
 *
 * A 20×20 grid of points connected by hairlines. Each point is a little spring: a pointer
 * within the interaction radius applies a repulsion force whose strength scales with
 * pointer velocity, and points that feel the push glow brighter. The whole layer renders
 * at 0.4 opacity behind every page, pointer-events-none, and runs on requestAnimationFrame
 * with full cleanup on unmount.
 *
 * On touch screens there is no hover; the grid follows the finger instead.
 */

const GRID = 20; // 20×20 points
const INTERACTION_RADIUS = 200; // px
const OPACITY = 0.4;

// Slate palette: slate-400 points (lifted inside the dark theme, drawn at low alpha),
// slate-700 lines, slate-600 glow on interaction.
const POINT_COLOR = "148,163,184";
const LINE_COLOR = "51,65,85";
const GLOW_COLOR = "71,85,105";

interface Point {
  x: number;
  y: number;
  homeX: number;
  homeY: number;
  vx: number;
  vy: number;
  glow: number;
}

export function CursorField() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;

    if (!canvas) {
      return;
    }

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }

    let width = 0;
    let height = 0;
    let cols = GRID;
    let rows = GRID;
    let points: Point[] = [];
    let pointerX = -1e4;
    let pointerY = -1e4;
    let pointerVX = 0;
    let pointerVY = 0;
    let lastPointerX = pointerX;
    let lastPointerY = pointerY;
    let lastPointerT = performance.now();
    let hasPointer = false;
    let rafId = 0;

    const resize = () => {
      const dpr = Math.min(window.devicePixelRatio || 1, 1.5);
      width = window.innerWidth;
      height = window.innerHeight;

      canvas.width = Math.floor(width * dpr);
      canvas.height = Math.floor(height * dpr);
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;

      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

      // Keep the grid roughly square: 20 columns, and as many rows as the aspect ratio needs.
      cols = GRID;
      rows = Math.max(8, Math.round((GRID * height) / Math.max(1, width)));

      points = [];
      for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
          const x = (col + 0.5) * (width / cols);
          const y = (row + 0.5) * (height / rows);

          points.push({ x, y, homeX: x, homeY: y, vx: 0, vy: 0, glow: 0 });
        }
      }
    };

    const onPointerMove = (event: PointerEvent) => {
      const now = performance.now();
      const dt = Math.max(1, now - lastPointerT);

      pointerVX = (event.clientX - lastPointerX) / dt;
      pointerVY = (event.clientY - lastPointerY) / dt;

      lastPointerX = event.clientX;
      lastPointerY = event.clientY;
      lastPointerT = now;

      pointerX = event.clientX;
      pointerY = event.clientY;
      hasPointer = true;
    };

    const onTouchMove = (event: TouchEvent) => {
      const touch = event.touches[0];
      if (touch) {
        pointerX = touch.clientX;
        pointerY = touch.clientY;
        hasPointer = true;
      }
    };

    const onPointerLeave = () => {
      hasPointer = false;
      pointerX = -1e4;
      pointerY = -1e4;
    };

    const frame = () => {
      const spring = 0.045;
      const damping = 0.86;

      for (const point of points) {
        if (hasPointer) {
          const dx = point.x - pointerX;
          const dy = point.y - pointerY;
          const distanceSq = dx * dx + dy * dy;

          if (distanceSq < INTERACTION_RADIUS * INTERACTION_RADIUS) {
            const distance = Math.max(1, Math.sqrt(distanceSq));
            const falloff = 1 - distance / INTERACTION_RADIUS;

            // Velocity feeds the repulsion strength, so a fast flick pushes harder.
            const speed = Math.min(1, Math.hypot(pointerVX, pointerVY) / 2);
            const force = falloff * (2.2 + speed * 4);

            point.vx += (dx / distance) * force;
            point.vy += (dy / distance) * force;
            point.glow = Math.min(1, point.glow + 0.25);
          } else {
            point.glow = Math.max(0, point.glow - 0.03);
          }
        } else {
          point.glow = Math.max(0, point.glow - 0.03);
        }

        // Damped spring toward the home position.
        point.vx = (point.vx + (point.homeX - point.x) * spring) * damping;
        point.vy = (point.vy + (point.homeY - point.y) * spring) * damping;

        point.x += point.vx;
        point.y += point.vy;
      }

      ctx.clearRect(0, 0, width, height);
      ctx.globalAlpha = OPACITY;

      // Hairline connections first, so points sit on top.
      ctx.strokeStyle = `rgb(${LINE_COLOR})`;
      ctx.lineWidth = 1;

      for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
          const index = row * cols + col;
          const point = points[index];

          if (col < cols - 1) {
            const right = points[index + 1];
            ctx.beginPath();
            ctx.moveTo(point.x, point.y);
            ctx.lineTo(right.x, right.y);
            ctx.stroke();
          }

          if (row < rows - 1) {
            const below = points[index + cols];
            ctx.beginPath();
            ctx.moveTo(point.x, point.y);
            ctx.lineTo(below.x, below.y);
            ctx.stroke();
          }
        }
      }

      // Points, with a glow that swells on interaction.
      for (const point of points) {
        ctx.beginPath();
        ctx.arc(point.x, point.y, 1.4, 0, Math.PI * 2);
        ctx.fillStyle = `rgb(${POINT_COLOR})`;
        ctx.fill();

        if (point.glow > 0.02) {
          ctx.beginPath();
          ctx.arc(point.x, point.y, 2.6 + point.glow * 4, 0, Math.PI * 2);
          ctx.fillStyle = `rgba(${GLOW_COLOR}, ${0.35 * point.glow})`;
          ctx.fill();
        }
      }

      ctx.globalAlpha = 1;

      rafId = requestAnimationFrame(frame);
    };

    resize();
    window.addEventListener("resize", resize);
    window.addEventListener("pointermove", onPointerMove, { passive: true });
    window.addEventListener("touchmove", onTouchMove, { passive: true });
    document.documentElement.addEventListener("pointerleave", onPointerLeave);

    rafId = requestAnimationFrame(frame);

    return () => {
      cancelAnimationFrame(rafId);
      window.removeEventListener("resize", resize);
      window.removeEventListener("pointermove", onPointerMove);
      window.removeEventListener("touchmove", onTouchMove);
      document.documentElement.removeEventListener("pointerleave", onPointerLeave);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      className="pointer-events-none fixed inset-0 z-0"
    />
  );
}