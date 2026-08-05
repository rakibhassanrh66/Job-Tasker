// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

/**
 * The API sends UTC timestamps without a trailing Z (System.Text.Json writes a DateTime
 * with Kind=Utc as an unqualified ISO string). Parsing that directly gives local time and
 * shifts every deadline by the viewer's offset, so it is qualified here before parsing.
 */
function parseUtc(iso: string): Date {
  const hasZone = /(?:Z|[+-]\d{2}:\d{2})$/.test(iso);

  return new Date(hasZone ? iso : `${iso}Z`);
}

export function formatDateTime(iso: string): string {
  return parseUtc(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function formatDate(iso: string): string {
  return parseUtc(iso).toLocaleDateString(undefined, { dateStyle: "medium" });
}

/** For a datetime-local input, which wants local time with no zone and no seconds. */
export function toLocalInputValue(iso: string): string {
  const date = parseUtc(iso);
  const offset = date.getTimezoneOffset() * 60_000;

  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

/** The inverse: a datetime-local value is local time, and the API wants UTC. */
export function fromLocalInputValue(value: string): string {
  return new Date(value).toISOString();
}

export function isPast(iso: string): boolean {
  return parseUtc(iso).getTime() < Date.now();
}

/** "in 3 days" / "2 hours ago", for deadlines where the exact timestamp matters less than
 *  whether there is still time. */
export function relativeToNow(iso: string): string {
  const deltaMs = parseUtc(iso).getTime() - Date.now();
  const formatter = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });

  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ["day", 86_400_000],
    ["hour", 3_600_000],
    ["minute", 60_000],
  ];

  for (const [unit, ms] of units) {
    if (Math.abs(deltaMs) >= ms) {
      return formatter.format(Math.trunc(deltaMs / ms), unit);
    }
  }

  return "now";
}
