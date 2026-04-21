"use client";

import { useMemo } from "react";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { useAuthStore } from "@/store/auth.store";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import {
  useChildAttendance,
  useChildResults
} from "@/features/parent-portal/hooks/use-parent-portal";
import type { Student } from "@/features/students/types/student.types";

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

function ProgressBar({ value, max = 100, color = "bg-brand-600" }: { value: number; max?: number; color?: string }) {
  const pct = Math.min(100, Math.round((value / max) * 100));
  return (
    <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
      <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
    </div>
  );
}

function ChildProgressContent({ child }: { child: Student }) {
  const attendanceQuery = useChildAttendance(child.id);
  const resultsQuery = useChildResults(child.id);

  const stats = useMemo(() => {
    const records = attendanceQuery.data?.items ?? [];
    const present = records.filter((r) => r.status === "Present").length;
    const late = records.filter((r) => r.status === "Late").length;
    const absent = records.filter((r) => r.status === "Absent").length;
    const pct = records.length > 0 ? Math.round(((present + late) / records.length) * 100) : 0;

    const results = resultsQuery.data?.items ?? [];
    const avgPct =
      results.length > 0
        ? Math.round(
            results.reduce((sum, r) => sum + (r.totalMarks > 0 ? (r.marksObtained / r.totalMarks) * 100 : 0), 0) /
              results.length
          )
        : 0;

    const gradeOrder = ["A+", "A", "B+", "B", "C+", "C", "D", "F"];
    const bestGrade = results.reduce<string | null>((acc, r) => {
      if (!acc) return r.grade;
      return gradeOrder.indexOf(r.grade) < gradeOrder.indexOf(acc) ? r.grade : acc;
    }, null);

    const subjectMap: Record<string, { total: number; marks: number; count: number }> = {};
    results.forEach((r) => {
      if (!subjectMap[r.subjectName]) {
        subjectMap[r.subjectName] = { total: 0, marks: 0, count: 0 };
      }
      subjectMap[r.subjectName].marks += r.marksObtained;
      subjectMap[r.subjectName].total += r.totalMarks;
      subjectMap[r.subjectName].count += 1;
    });

    const subjects = Object.entries(subjectMap).map(([name, data]) => ({
      name,
      pct: data.total > 0 ? Math.round((data.marks / data.total) * 100) : 0,
      exams: data.count
    }));

    return { pct, present, late, absent, total: records.length, avgPct, bestGrade, subjects };
  }, [attendanceQuery.data, resultsQuery.data]);

  const isLoading = attendanceQuery.isLoading || resultsQuery.isLoading;

  if (isLoading) {
    return <LoadingState title="Loading progress..." description="Fetching academic data." />;
  }

  if (attendanceQuery.isError || resultsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load progress"
        description="Academic progress data could not be loaded for the selected child."
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Attendance</p>
          <p className="mt-2 text-3xl font-semibold text-slate-950">{stats.pct}%</p>
          <div className="mt-3">
            <ProgressBar
              value={stats.pct}
              color={stats.pct >= 75 ? "bg-emerald-500" : "bg-rose-500"}
            />
          </div>
          <div className="mt-3 flex gap-3 text-xs text-slate-500">
            <span className="text-emerald-600">{stats.present} present</span>
            <span className="text-amber-600">{stats.late} late</span>
            <span className="text-rose-600">{stats.absent} absent</span>
          </div>
        </Card>

        <Card className="p-5">
          <p className="text-sm text-slate-500">Average score</p>
          <p className="mt-2 text-3xl font-semibold text-slate-950">{stats.avgPct}%</p>
          <div className="mt-3">
            <ProgressBar
              value={stats.avgPct}
              color={stats.avgPct >= 60 ? "bg-brand-600" : "bg-amber-500"}
            />
          </div>
          <p className="mt-3 text-xs text-slate-500">Across {resultsQuery.data?.items.length ?? 0} exams</p>
        </Card>

        <Card className="p-5">
          <p className="text-sm text-slate-500">Best grade</p>
          <p
            className={`mt-2 text-3xl font-semibold ${
              GRADE_COLORS[stats.bestGrade ?? ""]?.split(" ")[0] ?? "text-slate-950"
            }`}
          >
            {stats.bestGrade ?? "-"}
          </p>
          <p className="mt-3 text-xs text-slate-500">Highest achieved grade</p>
        </Card>
      </div>

      {stats.subjects.length > 0 ? (
        <Card className="p-6">
          <h3 className="font-semibold text-slate-900">Subject Performance</h3>
          <div className="mt-5 space-y-4">
            {stats.subjects
              .sort((a, b) => b.pct - a.pct)
              .map((subject) => (
                <div key={subject.name}>
                  <div className="flex items-center justify-between text-sm">
                    <span className="font-medium text-slate-700">{subject.name}</span>
                    <span className="text-slate-500">{subject.pct}% ({subject.exams} exam{subject.exams > 1 ? "s" : ""})</span>
                  </div>
                  <div className="mt-2">
                    <ProgressBar
                      value={subject.pct}
                      color={
                        subject.pct >= 75
                          ? "bg-emerald-500"
                          : subject.pct >= 50
                            ? "bg-brand-500"
                            : "bg-rose-500"
                      }
                    />
                  </div>
                </div>
              ))}
          </div>
        </Card>
      ) : null}
    </div>
  );
}

export function ParentChildProgressClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading child profile..." description="Fetching linked students." />;
  }

  if (childrenQuery.isError) {
    return (
      <EmptyState
        title="Unable to load children"
        description="Linked student profiles could not be loaded right now."
      />
    );
  }

  if (children.length === 0) {
    return (
      <EmptyState
        title="No child linked"
        description="No student is linked to your parent account. Contact the school administration."
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Progress Report"
        title="Academic Progress"
        description={`Overview for ${activeChild.fullName} — attendance rate, exam performance, and subject-by-subject results.`}
      />

      <ParentChildSwitcher students={children} value={activeChildId} onChange={setSelectedChildId} />

      {activeChild ? <ChildProgressContent child={activeChild} /> : null}
    </div>
  );
}
