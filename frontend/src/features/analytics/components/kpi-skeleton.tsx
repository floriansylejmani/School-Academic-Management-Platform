"use client";

import { useMemo } from "react";

// Deterministic pseudo-random based on seed — avoids Math.random() in render
function seededRandom(seed: number) {
  const x = Math.sin(seed + 1) * 10000;
  return x - Math.floor(x);
}

export function KpiSkeleton() {
  return (
    <div className="rounded-[28px] border border-slate-200 bg-white p-5 shadow-panel">
      <div className="flex items-start justify-between gap-3">
        <div className="h-4 w-28 animate-pulse rounded-full bg-slate-200" />
        <div className="h-9 w-9 animate-pulse rounded-2xl bg-slate-200" />
      </div>
      <div className="mt-4 space-y-2">
        <div className="h-8 w-24 animate-pulse rounded-full bg-slate-200" />
        <div className="h-3 w-36 animate-pulse rounded-full bg-slate-100" />
      </div>
    </div>
  );
}

export function ChartSkeleton({ className }: { className?: string }) {
  const heights = useMemo(
    () => Array.from({ length: 12 }, (_, i) => 30 + Math.sin(i * 0.8) * 25 + seededRandom(i) * 20),
    []
  );

  return (
    <div className={className}>
      <div className="mb-4 flex items-center justify-between">
        <div className="space-y-2">
          <div className="h-5 w-40 animate-pulse rounded-full bg-slate-200" />
          <div className="h-3 w-56 animate-pulse rounded-full bg-slate-100" />
        </div>
      </div>
      <div className="flex h-64 items-end gap-2">
        {heights.map((h, i) => (
          <div
            key={i}
            className="flex-1 animate-pulse rounded-t-lg bg-slate-100"
            style={{ height: `${h}%` }}
          />
        ))}
      </div>
    </div>
  );
}
