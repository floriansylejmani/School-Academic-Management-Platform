"use client";

import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip
} from "recharts";
import type { FinanceSummaryResponse } from "@/features/analytics/types/analytics.types";

interface FinanceSummaryChartProps {
  data: FinanceSummaryResponse;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value);
}

function CustomTooltip({
  active,
  payload
}: {
  active?: boolean;
  payload?: Array<{ name: string; value: number; payload: { color: string } }>;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const entry = payload[0];

  return (
    <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-panel">
      <div className="flex items-center gap-2">
        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: entry.payload.color }} />
        <span className="text-xs font-semibold text-slate-900">{entry.name}</span>
      </div>
      <p className="mt-1 text-sm font-semibold tabular-nums text-slate-900">
        {formatCurrency(entry.value)}
      </p>
    </div>
  );
}

const STATUS_CONFIG = [
  { key: "paidAmount",         label: "Paid",           color: "#10b981" },
  { key: "pendingAmount",      label: "Pending",        color: "#f59e0b" },
  { key: "overdueAmount",      label: "Overdue",        color: "#f43f5e" },
  { key: "partiallyPaidAmount",label: "Partially Paid", color: "#8b5cf6" }
] as const;

export function FinanceSummaryChart({ data }: FinanceSummaryChartProps) {
  const slices = STATUS_CONFIG
    .map((cfg) => ({
      name: cfg.label,
      value: data[cfg.key],
      color: cfg.color
    }))
    .filter((s) => s.value > 0);

  const legend = [
    { label: "Paid",           count: data.paidCount,          amount: data.paidAmount,          color: "#10b981" },
    { label: "Pending",        count: data.pendingCount,        amount: data.pendingAmount,        color: "#f59e0b" },
    { label: "Overdue",        count: data.overdueCount,        amount: data.overdueAmount,        color: "#f43f5e" },
    { label: "Partial",        count: data.partiallyPaidCount,  amount: data.partiallyPaidAmount,  color: "#8b5cf6" }
  ].filter((l) => l.count > 0);

  if (slices.length === 0) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-sm text-slate-400">No fee records found.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 md:flex-row md:items-center md:gap-8">
      <div className="shrink-0">
        <ResponsiveContainer width={180} height={180}>
          <PieChart>
            <Pie
              data={slices}
              cx="50%"
              cy="50%"
              innerRadius={52}
              outerRadius={80}
              paddingAngle={2}
              dataKey="value"
              strokeWidth={0}
            >
              {slices.map((entry, i) => (
                <Cell key={i} fill={entry.color} />
              ))}
            </Pie>
            <Tooltip content={<CustomTooltip />} />
          </PieChart>
        </ResponsiveContainer>
      </div>

      <div className="flex-1 space-y-2.5">
        {legend.map((item, i) => {
          const pct = data.totalFeesAmount === 0
            ? 0
            : Math.round((item.amount / data.totalFeesAmount) * 100);

          return (
            <div key={i}>
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
                  <span className="text-sm text-slate-600">{item.label}</span>
                  <span className="text-xs text-slate-400">({item.count})</span>
                </div>
                <span className="text-sm font-semibold tabular-nums text-slate-900">
                  {formatCurrency(item.amount)}
                </span>
              </div>
              <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
                <div
                  className="h-full rounded-full transition-all duration-500"
                  style={{ width: `${pct}%`, backgroundColor: item.color }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
