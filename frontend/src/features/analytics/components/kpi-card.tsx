import type { LucideIcon } from "lucide-react";
import { cn } from "@/utils/cn";

interface KpiCardProps {
  label: string;
  value: string;
  detail?: string;
  subDetail?: string;
  icon: LucideIcon;
  tone?: "default" | "success" | "warning" | "danger";
}

const toneStyles = {
  default: {
    icon: "bg-brand-50 text-brand-700",
    value: "text-slate-950"
  },
  success: {
    icon: "bg-emerald-50 text-emerald-700",
    value: "text-emerald-700"
  },
  warning: {
    icon: "bg-amber-50 text-amber-700",
    value: "text-amber-700"
  },
  danger: {
    icon: "bg-rose-50 text-rose-700",
    value: "text-rose-700"
  }
};

export function KpiCard({ label, value, detail, subDetail, icon: Icon, tone = "default" }: KpiCardProps) {
  const styles = toneStyles[tone];

  return (
    <div className="flex flex-col gap-4 rounded-[28px] border border-slate-200 bg-white p-5 shadow-panel">
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium text-slate-500">{label}</p>
        <div className={cn("shrink-0 rounded-2xl p-2.5", styles.icon)}>
          <Icon className="h-4.5 w-4.5 h-[18px] w-[18px]" />
        </div>
      </div>

      <div>
        <p className={cn("text-3xl font-semibold tabular-nums leading-none", styles.value)}>
          {value}
        </p>
        {detail ? (
          <p className="mt-2 text-xs text-slate-500">{detail}</p>
        ) : null}
        {subDetail ? (
          <p className="mt-1 text-xs text-slate-400">{subDetail}</p>
        ) : null}
      </div>
    </div>
  );
}
