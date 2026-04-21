"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import type { ExamPerformanceItem } from "@/features/analytics/types/analytics.types";

interface ExamPerformanceChartProps {
  examAverages: ExamPerformanceItem[];
}

function truncate(text: string, max: number) {
  return text.length > max ? `${text.slice(0, max)}…` : text;
}

function CustomTooltip({
  active,
  payload,
  label
}: {
  active?: boolean;
  payload?: Array<{ value: number; payload: ExamPerformanceItem }>;
  label?: string;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const item = payload[0].payload;

  return (
    <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-panel">
      <p className="mb-2 text-xs font-semibold text-slate-900">{label}</p>
      <p className="text-[11px] text-slate-500">
        {item.subjectName} · {item.className}
      </p>
      <div className="mt-2 space-y-1">
        <div className="flex justify-between gap-6">
          <span className="text-xs text-slate-500">Avg score</span>
          <span className="text-xs font-semibold tabular-nums text-slate-900">
            {item.averageScore} / {item.totalMarks}
          </span>
        </div>
        <div className="flex justify-between gap-6">
          <span className="text-xs text-emerald-600">Passed</span>
          <span className="text-xs font-semibold tabular-nums text-emerald-700">{item.passCount}</span>
        </div>
        <div className="flex justify-between gap-6">
          <span className="text-xs text-rose-500">Failed</span>
          <span className="text-xs font-semibold tabular-nums text-rose-600">{item.failCount}</span>
        </div>
      </div>
    </div>
  );
}

function getBarColor(item: ExamPerformanceItem) {
  const rate = item.totalMarks === 0 ? 0 : item.averageScore / item.totalMarks;
  if (rate >= 0.75) return "#10b981";
  if (rate >= 0.5)  return "#0d67d1";
  return "#f43f5e";
}

export function ExamPerformanceChart({ examAverages }: ExamPerformanceChartProps) {
  if (examAverages.length === 0) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-sm text-slate-400">No exam results recorded yet.</p>
      </div>
    );
  }

  // Normalise bars to percentage of total marks so axes are comparable across exams
  const chartData = examAverages.map((item) => ({
    ...item,
    scorePercent: item.totalMarks === 0 ? 0 : Math.round((item.averageScore / item.totalMarks) * 100),
    shortTitle: truncate(item.examTitle, 14)
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={chartData} margin={{ top: 4, right: 4, left: -24, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" horizontal vertical={false} />
        <XAxis
          dataKey="shortTitle"
          tick={{ fontSize: 11, fill: "#94a3b8" }}
          axisLine={false}
          tickLine={false}
        />
        <YAxis
          domain={[0, 100]}
          tickFormatter={(v: number) => `${v}%`}
          tick={{ fontSize: 11, fill: "#94a3b8" }}
          axisLine={false}
          tickLine={false}
        />
        {/* Pass threshold reference line at 50% */}
        <ReferenceLine
          y={50}
          stroke="#f43f5e"
          strokeDasharray="4 3"
          strokeWidth={1.5}
          label={{ value: "Pass (50%)", position: "insideTopRight", fontSize: 10, fill: "#f43f5e" }}
        />
        <Tooltip content={<CustomTooltip />} cursor={{ fill: "rgba(15,23,42,0.04)" }} />
        <Bar dataKey="scorePercent" radius={[6, 6, 0, 0]} maxBarSize={52}>
          {chartData.map((entry, i) => (
            <Cell key={i} fill={getBarColor(entry)} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
