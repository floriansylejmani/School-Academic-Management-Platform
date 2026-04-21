"use client";

import {
  BookOpen,
  ClipboardCheck,
  GraduationCap,
  ReceiptText
} from "lucide-react";
import { useMemo } from "react";
import { Card } from "@/components/ui/card";
import { DemoEmptyState, RoleDemoCTA, EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { NotificationsSummaryCard } from "@/features/notifications/components/notifications-summary-card";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentDashboardOverview } from "@/features/parent-portal/hooks/use-parent-portal";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import { useAuthStore } from "@/store/auth.store";

const GRADE_COLORS: Record<string, string> = {
  "A+": "text-emerald-700 bg-emerald-50",
  A: "text-emerald-600 bg-emerald-50",
  "B+": "text-blue-700 bg-blue-50",
  B: "text-blue-600 bg-blue-50",
  "C+": "text-amber-700 bg-amber-50",
  C: "text-amber-600 bg-amber-50",
  D: "text-orange-600 bg-orange-50",
  F: "text-rose-700 bg-rose-50"
};

const FEE_STATUS_COLORS: Record<string, string> = {
  Paid: "text-emerald-700 bg-emerald-50",
  Pending: "text-amber-700 bg-amber-50",
  Overdue: "text-rose-700 bg-rose-50",
  PartiallyPaid: "text-blue-700 bg-blue-50"
};

function getFeeStatusLabel(status: string) {
  return status === "PartiallyPaid" ? "Partial" : status;
}

function StatCard({
  label,
  value,
  icon: Icon,
  accent = false
}: {
  label: string;
  value: string;
  icon: React.ElementType;
  accent?: boolean;
}) {
  return (
    <Card className="p-6">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-slate-500">{label}</p>
          <p className="mt-3 text-3xl font-semibold tabular-nums text-slate-950">{value}</p>
        </div>
        <div className={`shrink-0 rounded-2xl p-3 ${accent ? "bg-brand-700 text-white" : "bg-brand-50 text-brand-700"}`}>
          <Icon className="h-5 w-5" />
        </div>
      </div>
    </Card>
  );
}

export function ParentDashboardClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);
  const overviewQuery = useParentDashboardOverview(children);

  const summary = useMemo(() => {
    const allAttendance = Object.values(overviewQuery.data?.attendanceByStudentId ?? {}).flat();
    const allFees = Object.values(overviewQuery.data?.feesByStudentId ?? {}).flat();
    const allExams = Object.values(overviewQuery.data?.examsByClassId ?? {}).flat();

    const presentCount = allAttendance.filter(
      (record) => record.status === "Present" || record.status === "Late"
    ).length;
    const attendancePct =
      allAttendance.length > 0 ? `${Math.round((presentCount / allAttendance.length) * 100)}%` : "-";

    const today = new Date();
    const upcomingExams = allExams.filter(
      (exam) => new Date(exam.examDate) >= new Date(today.toDateString())
    );
    const outstandingFees = allFees.filter((fee) => fee.status !== "Paid").length;

    return {
      linkedChildren: String(children.length),
      attendancePct,
      upcomingExams: String(upcomingExams.length),
      outstandingFees: String(outstandingFees),
      selectedChildResults: [...(activeChild ? overviewQuery.data?.resultsByStudentId[activeChild.id] ?? [] : [])]
        .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
        .slice(0, 4),
      selectedChildFees: (activeChild ? overviewQuery.data?.feesByStudentId[activeChild.id] ?? [] : [])
        .filter((fee) => fee.status !== "Paid")
        .sort((left, right) => new Date(left.dueDate).getTime() - new Date(right.dueDate).getTime())
        .slice(0, 3),
      selectedChildExams: [...(activeChild?.classId ? overviewQuery.data?.examsByClassId[activeChild.classId] ?? [] : [])]
        .filter((exam) => new Date(exam.examDate) >= new Date(today.toDateString()))
        .sort((left, right) => new Date(left.examDate).getTime() - new Date(right.examDate).getTime())
        .slice(0, 3)
    };
  }, [activeChild, children.length, overviewQuery.data]);

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading family portal..." description="Fetching linked student profiles." />;
  }

  if (childrenQuery.isError) {
    return (
      <EmptyState
        title="Unable to load child profiles"
        description="Linked student profiles could not be loaded. Please contact the school administration."
      />
    );
  }

  if (children.length === 0) {
    return (
      <div className="space-y-6">
        <DemoEmptyState
          feature="Parent Monitoring"
          description="This demo includes sample parent accounts linked to student profiles. Try logging in as a parent to explore child progress tracking, fee payments, and academic monitoring features."
          demoAction={<RoleDemoCTA role="parent" />}
        />
      </div>
    );
  }

  if (overviewQuery.isLoading) {
    return <LoadingState title="Loading dashboard..." description="Fetching family academic data." />;
  }

  if (overviewQuery.isError) {
    return (
      <EmptyState
        title="Unable to load dashboard"
        description="Parent dashboard data could not be loaded right now."
      />
    );
  }

  return (
    <div className="space-y-6">
      <Card className="overflow-hidden">
        <div className="bg-dashboard-glow px-6 py-8 lg:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Family Portal</p>
          <h2 className="mt-3 text-3xl font-semibold text-slate-950">
            Welcome, {user?.fullName?.split(" ")[0] ?? "Parent"}
          </h2>
          <p className="mt-4 max-w-2xl text-sm leading-7 text-slate-600">
            {children.length === 1
              ? `Monitoring ${activeChild?.fullName ?? "your child"} in ${activeChild?.className ?? "their class"}.`
              : `Monitoring ${children.length} linked children with a shared family overview and per-child detail views.`}
          </p>
        </div>
      </Card>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-slate-700">Selected child</p>
          <p className="mt-1 text-sm text-slate-500">
            {activeChild?.fullName ?? "No child selected"}
            {activeChild?.className ? ` - ${activeChild.className}` : ""}
          </p>
        </div>
        <ParentChildSwitcher
          students={children}
          value={activeChildId}
          onChange={setSelectedChildId}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Linked children" value={summary.linkedChildren} icon={GraduationCap} />
        <StatCard label="Family attendance" value={summary.attendancePct} icon={ClipboardCheck} />
        <StatCard label="Upcoming exams" value={summary.upcomingExams} icon={BookOpen} accent />
        <StatCard label="Outstanding fees" value={summary.outstandingFees} icon={ReceiptText} />
      </div>

      <NotificationsSummaryCard
        title="Recent notifications"
        description="Updates about attendance, exams, fees, and results across your linked children."
        href="/parent/notifications"
        emptyMessage="No notifications have been delivered yet."
      />

      <div className="grid gap-6 xl:grid-cols-3">
        <Card className="p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Academic</p>
          <h3 className="mt-2 text-lg font-semibold text-slate-950">Recent Results</h3>
          {summary.selectedChildResults.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">No results recorded yet.</p>
          ) : (
            <div className="mt-4 space-y-2.5">
              {summary.selectedChildResults.map((result) => (
                <div
                  key={result.id}
                  className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3"
                >
                  <div>
                    <p className="text-sm font-medium text-slate-900">{result.examTitle}</p>
                    <p className="text-xs text-slate-500">{result.subjectName}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-slate-600">
                      {result.marksObtained}/{result.totalMarks}
                    </span>
                    <span
                      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${
                        GRADE_COLORS[result.grade] ?? "bg-slate-100 text-slate-600"
                      }`}
                    >
                      {result.grade}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card className="p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Exams</p>
          <h3 className="mt-2 text-lg font-semibold text-slate-950">Upcoming Exams</h3>
          {summary.selectedChildExams.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">No upcoming exams scheduled.</p>
          ) : (
            <div className="mt-4 space-y-2.5">
              {summary.selectedChildExams.map((exam) => (
                <div
                  key={exam.id}
                  className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3"
                >
                  <div>
                    <p className="text-sm font-medium text-slate-900">{exam.title}</p>
                    <p className="text-xs text-slate-500">{exam.subjectName}</p>
                  </div>
                  <span className="text-sm font-medium text-slate-700">{exam.examDate}</span>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card className="p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Fees</p>
          <h3 className="mt-2 text-lg font-semibold text-slate-950">Outstanding Fees</h3>
          {summary.selectedChildFees.length === 0 ? (
            <p className="mt-3 text-sm font-medium text-emerald-600">All fees are paid.</p>
          ) : (
            <div className="mt-4 space-y-2.5">
              {summary.selectedChildFees.map((fee) => (
                <div
                  key={fee.id}
                  className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3"
                >
                  <div>
                    <p className="text-sm font-medium text-slate-900">{fee.feeType}</p>
                    <p className="text-xs text-slate-500">Due: {fee.dueDate}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-semibold text-slate-700">${fee.amount.toFixed(2)}</span>
                    <span
                      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${
                        FEE_STATUS_COLORS[fee.status] ?? "bg-slate-100 text-slate-600"
                      }`}
                    >
                      {getFeeStatusLabel(fee.status)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
