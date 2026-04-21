import { ArrowUpRight, BookCopy, ClipboardCheck, GraduationCap, Users } from "lucide-react";
import { Card } from "@/components/ui/card";

interface DashboardOverviewProps {
  title: string;
  description: string;
  metrics: Array<{
    label: string;
    value: string;
    change: string;
    icon: "students" | "teachers" | "attendance" | "subjects";
  }>;
}

const iconMap = {
  students: GraduationCap,
  teachers: Users,
  attendance: ClipboardCheck,
  subjects: BookCopy
};

export function DashboardOverview({ title, description, metrics }: DashboardOverviewProps) {
  return (
    <div className="space-y-6">
      <Card className="overflow-hidden">
        <div className="grid gap-8 bg-dashboard-glow px-6 py-8 lg:grid-cols-[1.3fr_0.7fr] lg:px-8">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Overview</p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950">{title}</h2>
            <p className="mt-4 max-w-2xl text-sm leading-7 text-slate-600">{description}</p>
          </div>

          <div className="rounded-[28px] bg-slate-950 p-6 text-white shadow-2xl shadow-slate-950/15">
            <p className="text-sm text-white/70">Platform</p>
            <p className="mt-3 text-xl font-semibold leading-snug">Scholara Academic Platform</p>
            <p className="mt-3 text-sm leading-6 text-white/70">
              All modules are live. Data is scoped to your access level and refreshes automatically.
            </p>
          </div>
        </div>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric) => {
          const Icon = iconMap[metric.icon];

          return (
            <Card key={metric.label} className="p-5">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm text-slate-500">{metric.label}</p>
                  <p className="mt-3 text-3xl font-semibold text-slate-950">{metric.value}</p>
                </div>
                <div className="rounded-2xl bg-brand-50 p-3 text-brand-700">
                  <Icon className="h-5 w-5" />
                </div>
              </div>
              <div className="mt-6 inline-flex items-center gap-2 text-sm font-medium text-emerald-600">
                <ArrowUpRight className="h-4 w-4" />
                {metric.change}
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
