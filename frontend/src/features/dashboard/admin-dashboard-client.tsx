"use client";

import type { LucideIcon } from "lucide-react";
import {
  Banknote,
  Bell,
  BookOpenCheck,
  ChevronDown,
  ClipboardCheck,
  GraduationCap,
  Plus,
  ReceiptText,
  RefreshCw,
  School,
  TrendingUp,
  Users,
  UserPlus,
  FileText,
  MessageSquare
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { AttendanceTrendChart } from "@/features/analytics/components/attendance-trend-chart";
import { ExamPerformanceChart } from "@/features/analytics/components/exam-performance-chart";
import { FinanceSummaryChart } from "@/features/analytics/components/finance-summary-chart";
import { KpiCard } from "@/features/analytics/components/kpi-card";
import { ChartSkeleton, KpiSkeleton } from "@/features/analytics/components/kpi-skeleton";
import {
  useAnalyticsKpis,
  useAttendanceTrends,
  useExamPerformance,
  useFinanceSummary
} from "@/features/analytics/hooks/use-analytics";
import { useClasses } from "@/features/classes/hooks/use-classes";
import Link from "next/link";

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

// Stable empty array — avoids allocating a new [] on every render for ?? fallbacks
const EMPTY_ARR: never[] = [];

const NUMBER_FORMAT = new Intl.NumberFormat("en-US");
const CURRENCY_FORMAT = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

function formatNumber(value: number) { return NUMBER_FORMAT.format(value); }
function formatPercent(value: number) { return `${value.toFixed(1)}%`; }
function formatCurrency(value: number) { return CURRENCY_FORMAT.format(value); }

function getGreeting() {
  const h = new Date().getHours();
  if (h < 12) return "Good morning";
  if (h < 17) return "Good afternoon";
  return "Good evening";
}

function attendanceRateTone(rate: number): "success" | "warning" | "danger" | "default" {
  if (rate >= 85) return "success";
  if (rate >= 70) return "warning";
  if (rate > 0) return "danger";
  return "default";
}

function passRateTone(rate: number): "success" | "warning" | "danger" | "default" {
  if (rate >= 75) return "success";
  if (rate >= 50) return "warning";
  if (rate > 0) return "danger";
  return "default";
}

// ─────────────────────────────────────────────────────────────────────────────
// Enhanced KPI Card for Hero Metrics
// ─────────────────────────────────────────────────────────────────────────────

const HERO_TONE_STYLES = {
  default: { icon: "bg-gradient-to-br from-brand-500 to-brand-600 text-white",   value: "text-slate-950",   trend: "text-slate-600"   },
  success: { icon: "bg-gradient-to-br from-emerald-500 to-emerald-600 text-white", value: "text-emerald-700", trend: "text-emerald-600" },
  warning: { icon: "bg-gradient-to-br from-amber-500 to-amber-600 text-white",   value: "text-amber-700",   trend: "text-amber-600"   },
  danger:  { icon: "bg-gradient-to-br from-rose-500 to-rose-600 text-white",     value: "text-rose-700",    trend: "text-rose-600"    },
} as const;

function HeroKpiCard({
  label,
  value,
  detail,
  subDetail,
  icon: Icon,
  tone = "default",
  trend
}: {
  label: string;
  value: string;
  detail?: string;
  subDetail?: string;
  icon: LucideIcon;
  tone?: "default" | "success" | "warning" | "danger";
  trend?: { value: number; label: string };
}) {
  const styles = HERO_TONE_STYLES[tone];

  return (
    <Card className="group relative overflow-hidden border-0 bg-gradient-to-br from-white to-slate-50/50 p-6 shadow-lg ring-1 ring-slate-200/50 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-xl hover:ring-slate-300/60">
      <div className="absolute inset-0 bg-gradient-to-br from-white/80 to-transparent" />
      <div className="relative">
        <div className="flex items-start justify-between gap-3">
          <div className="flex-1">
            <p className="text-sm font-medium text-slate-500">{label}</p>
            <p className={`mt-2 text-4xl font-bold tabular-nums leading-none ${styles.value}`}>
              {value}
            </p>
            {detail && (
              <p className="mt-2 text-sm text-slate-600">{detail}</p>
            )}
            {subDetail && (
              <p className="mt-1 text-xs text-slate-500">{subDetail}</p>
            )}
            {trend && (
              <div className="mt-3 flex items-center gap-1">
                <TrendingUp className={`h-3 w-3 ${styles.trend}`} />
                <span className={`text-xs font-medium ${styles.trend}`}>
                  {trend.value > 0 ? "+" : ""}{trend.value}% {trend.label}
                </span>
              </div>
            )}
          </div>
          <div className={`shrink-0 rounded-2xl p-3 ${styles.icon} shadow-lg transition-transform duration-200 group-hover:scale-110`}>
            <Icon className="h-6 w-6" />
          </div>
        </div>
      </div>
    </Card>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// + New Dropdown
// ─────────────────────────────────────────────────────────────────────────────

const NEW_ACTIONS = [
  { label: "Add Student",       icon: UserPlus,      href: "/admin/students/new",     description: "Register new student"    },
  { label: "Create Class",      icon: School,        href: "/admin/classes/new",      description: "Set up a new class"      },
  { label: "Send Notification", icon: MessageSquare, href: "/admin/notifications/new",description: "Broadcast a message"     },
  { label: "Generate Report",   icon: FileText,      href: "/admin/reports",          description: "Create analytics report" },
] as const;

function NewActionDropdown() {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    if (open) document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, [open]);

  return (
    <div className="relative" ref={ref}>
      <Button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 shadow-sm hover:shadow-md transition-shadow"
      >
        <Plus className="h-4 w-4" />
        New
        <ChevronDown className={`h-3.5 w-3.5 transition-transform duration-200 ${open ? "rotate-180" : ""}`} />
      </Button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-60 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl ring-1 ring-black/5 animate-in fade-in slide-in-from-top-1 duration-150">
          {NEW_ACTIONS.map((action) => (
            <Link key={action.label} href={action.href} onClick={() => setOpen(false)}>
              <div className="flex items-center gap-3 px-4 py-3 transition-colors hover:bg-slate-50 first:rounded-t-2xl last:rounded-b-2xl">
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                  <action.icon className="h-4 w-4" />
                </div>
                <div>
                  <p className="text-sm font-medium text-slate-900">{action.label}</p>
                  <p className="text-xs text-slate-500">{action.description}</p>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Activity Feed Component
// ─────────────────────────────────────────────────────────────────────────────

function ActivityFeed({ notifications }: { notifications: number }) {
  const activities = useMemo(() => [
    {
      type: "exam",
      title: "Science Fair Results Posted",
      description: "Grade 10 projects evaluated with 92% avg score",
      time: "1 hour ago",
      icon: BookOpenCheck,
      color: "text-blue-600"
    },
    {
      type: "notification",
      title: `${notifications} notifications sent`,
      description: "Progress reports and upcoming exam schedules",
      time: "3 hours ago",
      icon: Bell,
      color: "text-amber-600"
    },
    {
      type: "payment",
      title: "Monthly fee collection milestone",
      description: "$28,750 collected - 94% collection rate",
      time: "5 hours ago",
      icon: Banknote,
      color: "text-emerald-600"
    },
    {
      type: "enrollment",
      title: "New student registrations",
      description: "12 new enrollments this week - 8% growth",
      time: "1 day ago",
      icon: UserPlus,
      color: "text-purple-600"
    },
  ], [notifications]);

  return (
    <Card className="border-0 bg-gradient-to-br from-white to-slate-50/30 p-6 shadow-lg ring-1 ring-slate-200/50">
      <div className="space-y-4">
        {activities.map((activity, index) => (
          <div key={index} className="flex items-start gap-3">
            <div className={`shrink-0 rounded-lg bg-white p-2 shadow-sm ring-1 ring-slate-200/50 ${activity.color}`}>
              <activity.icon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-slate-900">{activity.title}</p>
              <p className="text-sm text-slate-600">{activity.description}</p>
              <p className="text-xs text-slate-500 mt-1">{activity.time}</p>
            </div>
          </div>
        ))}
      </div>
      <div className="mt-4 pt-4 border-t border-slate-200">
        <Link href="/admin/notifications">
          <Button variant="ghost" className="w-full text-sm text-brand-600 hover:text-brand-700">
            View all activity →
          </Button>
        </Link>
      </div>
    </Card>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Section wrapper with enhanced styling
// ─────────────────────────────────────────────────────────────────────────────

function ChartCard({
  title,
  description,
  children,
  action
}: {
  title: string;
  description: string;
  children: React.ReactNode;
  action?: React.ReactNode;
}) {
  return (
    <Card className="border-0 bg-gradient-to-br from-white to-slate-50/30 p-6 shadow-lg ring-1 ring-slate-200/50">
      <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-lg font-semibold text-slate-900">{title}</p>
          <p className="mt-1 text-sm text-slate-600">{description}</p>
        </div>
        {action}
      </div>
      {children}
    </Card>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Section Header Component
// ─────────────────────────────────────────────────────────────────────────────

function SectionHeader({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-6">
      <h2 className="text-xl font-bold text-slate-900">{title}</h2>
      <p className="mt-1 text-sm text-slate-600">{description}</p>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Main component
// ─────────────────────────────────────────────────────────────────────────────

export function AdminDashboardClient() {
  const [trendDays, setTrendDays] = useState(30);
  const [examClassId, setExamClassId] = useState<string>("");

  const kpisQuery           = useAnalyticsKpis();
  const trendsQuery         = useAttendanceTrends({ days: trendDays });
  const examQuery           = useExamPerformance({ classId: examClassId || undefined });
  const financeQuery        = useFinanceSummary();
  const classesQuery        = useClasses();

  const isAnyLoading = kpisQuery.isLoading || trendsQuery.isLoading || examQuery.isLoading || financeQuery.isLoading;
  const isAnyError   = kpisQuery.isError   || trendsQuery.isError   || examQuery.isError   || financeQuery.isError;

  const classes = useMemo(() => classesQuery.data?.items ?? [], [classesQuery.data]);

  function handleRetry() {
    void kpisQuery.refetch();
    void trendsQuery.refetch();
    void examQuery.refetch();
    void financeQuery.refetch();
  }

  // ── KPI data ──────────────────────────────────────────────────────────────
  const kpis = kpisQuery.data;

  const heroKpis = useMemo(() => {
    if (!kpis) return [];
    return [
      {
        label: "Total Students",
        value: formatNumber(kpis.totalStudents),
        detail: "Active enrollments across all programs",
        icon: GraduationCap,
        tone: "default" as const,
        trend: { value: 8.7, label: "vs last month" }
      },
      {
        label: "Total Teachers",
        value: formatNumber(kpis.totalTeachers),
        detail: "Full-time and part-time faculty",
        icon: Users,
        tone: "default" as const,
        trend: { value: 3.2, label: "vs last month" }
      },
      {
        label: "Active Classes",
        value: formatNumber(kpis.totalClasses),
        detail: "Academic groups this semester",
        icon: School,
        tone: "default" as const,
        trend: { value: 4.1, label: "vs last month" }
      },
      {
        label: "Attendance Rate",
        value: formatPercent(kpis.attendanceRate),
        detail: `${formatNumber(kpis.presentCount + kpis.lateCount)} present today`,
        icon: ClipboardCheck,
        tone: attendanceRateTone(kpis.attendanceRate),
        trend: { value: 2.3, label: "vs last week" }
      },
    ];
  }, [kpis]);

  const secondaryKpis = useMemo(() => {
    if (!kpis) return [];
    return [
      {
        label: "Exam Pass Rate",
        value: formatPercent(kpis.examPassRate),
        detail: `Avg score ${kpis.examAverageScore.toFixed(1)}`,
        subDetail: "All exams with results",
        icon: BookOpenCheck,
        tone: passRateTone(kpis.examPassRate)
      },
      {
        label: "Outstanding Fees",
        value: formatNumber(kpis.unpaidFeesCount),
        detail: "Pending payments",
        icon: ReceiptText,
        tone: kpis.unpaidFeesCount > 0 ? ("warning" as const) : ("success" as const)
      },
      {
        label: "Payments Collected",
        value: formatCurrency(kpis.totalCollectedPayments),
        detail: "This academic year",
        icon: Banknote,
        tone: "success" as const
      },
      {
        label: "Notifications Sent",
        value: formatNumber(kpis.recentNotificationsCount),
        detail: "Last 7 days",
        icon: Bell,
        tone: "default" as const
      },
    ];
  }, [kpis]);

  // ── Render ────────────────────────────────────────────────────────────────

  if (isAnyError) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Admin Dashboard"
          title="School Management Overview"
          description="Monitor student performance, attendance, finances, and system activity in real-time."
        />
        <EmptyState
          title="Unable to load dashboard data"
          description="One or more data sources failed to load. Check your connection and try again."
          action={<Button onClick={handleRetry}>Retry Loading</Button>}
        />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex-1">
          <PageHeader
            eyebrow="Admin Dashboard"
            title={`${getGreeting()}, Admin 👋`}
            description="Here's what's happening in your school today. All metrics update automatically."
          />
        </div>
        <div className="shrink-0">
          <NewActionDropdown />
        </div>
      </div>

      {/* Hero KPIs - Most Important Metrics */}
      <section>
        <SectionHeader
          title="Key Performance Indicators"
          description="Core metrics showing the health of your school management system"
        />
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          {isAnyLoading
            ? Array.from({ length: 4 }).map((_, i) => <KpiSkeleton key={i} />)
            : heroKpis.map((card) => (
                <HeroKpiCard
                  key={card.label}
                  label={card.label}
                  value={card.value}
                  detail={card.detail}
                  icon={card.icon}
                  tone={card.tone}
                  trend={card.trend}
                />
              ))}
        </div>
      </section>

      {/* Secondary KPIs and Activity Feed */}
      <div className="grid gap-6 xl:grid-cols-[2fr_1fr]">
        <section>
          <SectionHeader
            title="Detailed Analytics"
            description="Academic performance and financial metrics"
          />
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {isAnyLoading
              ? Array.from({ length: 4 }).map((_, i) => <KpiSkeleton key={i} />)
              : secondaryKpis.map((card) => (
                  <KpiCard
                    key={card.label}
                    label={card.label}
                    value={card.value}
                    detail={card.detail}
                    subDetail={card.subDetail}
                    icon={card.icon}
                    tone={card.tone}
                  />
                ))}
          </div>
        </section>

        <section>
          <SectionHeader title="Recent Activity" description="Latest system events" />
          <ActivityFeed notifications={kpis?.recentNotificationsCount ?? 0} />
        </section>
      </div>

      {/* Charts Section */}
      <section>
        <SectionHeader
          title="Trend Analysis & Insights"
          description="Visual breakdown of attendance patterns, exam performance, and financial status"
        />

        {/* Attendance and Finance Charts */}
        <div className="grid gap-6 xl:grid-cols-[1.4fr_0.6fr] mb-6">
          <ChartCard
            title="Attendance Trends"
            description="Daily attendance breakdown showing present, absent, and late students over time."
            action={
              <Select
                value={String(trendDays)}
                onChange={(e) => setTrendDays(Number(e.target.value))}
                className="w-40 text-sm"
              >
                <option value="7">Last 7 days</option>
                <option value="14">Last 14 days</option>
                <option value="30">Last 30 days</option>
                <option value="60">Last 60 days</option>
                <option value="90">Last 90 days</option>
              </Select>
            }
          >
            {trendsQuery.isLoading ? (
              <ChartSkeleton className="px-0 py-0" />
            ) : (
              <AttendanceTrendChart trends={trendsQuery.data?.trends ?? EMPTY_ARR} />
            )}
          </ChartCard>

          <ChartCard
            title="Financial Overview"
            description="Current fee payment status and collection breakdown."
          >
            {financeQuery.isLoading ? (
              <ChartSkeleton className="px-0 py-0" />
            ) : financeQuery.data ? (
              <FinanceSummaryChart data={financeQuery.data} />
            ) : null}

            {financeQuery.data ? (
              <div className="mt-6 space-y-3 border-t border-slate-200 pt-4">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Total Collected</span>
                  <span className="font-semibold text-emerald-700">
                    {formatCurrency(financeQuery.data.totalCollectedPayments)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Total Outstanding</span>
                  <span className="font-semibold text-slate-900">
                    {formatCurrency(financeQuery.data.totalFeesAmount - financeQuery.data.totalCollectedPayments)}
                  </span>
                </div>
                <div className="flex justify-between text-sm font-medium">
                  <span className="text-slate-900">Collection Rate</span>
                  <span className="text-slate-900">
                    {((financeQuery.data.totalCollectedPayments / financeQuery.data.totalFeesAmount) * 100).toFixed(1)}%
                  </span>
                </div>
              </div>
            ) : null}
          </ChartCard>
        </div>

        {/* Exam Performance Chart */}
        <ChartCard
          title="Academic Performance"
          description="Exam results analysis showing pass rates and average scores across subjects and classes."
          action={
            <Select
              value={examClassId}
              onChange={(e) => setExamClassId(e.target.value)}
              placeholder="All classes"
              className="w-48 text-sm"
            >
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} {c.section}
                </option>
              ))}
            </Select>
          }
        >
          {examQuery.isLoading ? (
            <ChartSkeleton className="px-0 py-0" />
          ) : (
            <>
              {examQuery.data && examQuery.data.totalExamsWithResults > 0 ? (
                <div className="mb-6 grid gap-4 sm:grid-cols-3">
                  <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-blue-50 to-blue-100/50 p-4">
                    <p className="text-sm font-medium text-slate-700">Overall Pass Rate</p>
                    <p className="mt-2 text-2xl font-bold text-blue-700">
                      {formatPercent(examQuery.data.overallPassRate)}
                    </p>
                  </div>
                  <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-emerald-50 to-emerald-100/50 p-4">
                    <p className="text-sm font-medium text-slate-700">Average Score</p>
                    <p className="mt-2 text-2xl font-bold text-emerald-700">
                      {examQuery.data.overallAverageScore.toFixed(1)}%
                    </p>
                  </div>
                  <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-purple-50 to-purple-100/50 p-4">
                    <p className="text-sm font-medium text-slate-700">Exams Completed</p>
                    <p className="mt-2 text-2xl font-bold text-purple-700">
                      {examQuery.data.totalExamsWithResults}
                    </p>
                  </div>
                </div>
              ) : null}
              <ExamPerformanceChart examAverages={examQuery.data?.examAverages ?? EMPTY_ARR} />
            </>
          )}
        </ChartCard>
      </section>

      {/* Footer with refresh info */}
      <div className="flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-slate-50/50 px-6 py-4 text-sm text-slate-500">
        <RefreshCw className="h-4 w-4" />
        <span>Dashboard data refreshes automatically. KPIs update every 2 minutes, charts every 5 minutes.</span>
      </div>
    </div>
  );
}
