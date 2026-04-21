"use client";

import { BookOpen, CalendarDays, ClipboardCheck, GraduationCap } from "lucide-react";
import { useMemo } from "react";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { NotificationsSummaryCard } from "@/features/notifications/components/notifications-summary-card";
import { useAuthStore } from "@/store/auth.store";
import { useStudentProfile } from "@/features/profile/hooks/use-profile";
import {
  useStudentAttendance,
  useStudentExams,
  useStudentResults,
  useStudentTimetable
} from "@/features/student-portal/hooks/use-student-portal";

const DAYS_OF_WEEK = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

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

function InlineLoader() {
  return (
    <div className="flex items-center gap-2 py-4 text-sm text-slate-500">
      <div className="h-4 w-4 animate-spin rounded-full border-2 border-slate-200 border-t-brand-600" />
      Loading...
    </div>
  );
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

export function StudentDashboardClient() {
  const { user } = useAuthStore();
  const profileQuery = useStudentProfile();

  const student = profileQuery.data;
  const studentId = student?.id;
  const classId = student?.classId ?? undefined;

  const attendanceQuery = useStudentAttendance(studentId);
  const resultsQuery = useStudentResults(studentId);
  const timetableQuery = useStudentTimetable(classId);
  const examsQuery = useStudentExams(classId);

  const todayName = DAYS_OF_WEEK[new Date().getDay()];

  const stats = useMemo(() => {
    const records = attendanceQuery.data?.items ?? [];
    const presentCount = records.filter(
      (r) => r.status === "Present" || r.status === "Late"
    ).length;
    const attendancePct =
      records.length > 0
        ? `${Math.round((presentCount / records.length) * 100)}%`
        : "-";

    const totalExams = examsQuery.data?.items.length ?? 0;

    const results = resultsQuery.data?.items ?? [];
    const latestResult = [...results].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )[0];
    const latestGrade = latestResult?.grade ?? "-";

    const timetableEntries = timetableQuery.data?.items ?? [];
    const todayCount = timetableEntries.filter(
      (e) => e.dayOfWeek === todayName
    ).length;

    return { attendancePct, totalExams, latestGrade, todayCount, latestResult, timetableEntries, results };
  }, [attendanceQuery.data, resultsQuery.data, timetableQuery.data, examsQuery.data, todayName]);

  if (profileQuery.isLoading) {
    return <LoadingState title="Loading your profile..." description="Fetching your student details." />;
  }

  if (profileQuery.isError) {
    return (
      <EmptyState
        title="Profile unavailable"
        description="Your student profile could not be loaded. Please contact the admin."
      />
    );
  }

  const todayEntries = (timetableQuery.data?.items ?? [])
    .filter((e) => e.dayOfWeek === todayName)
    .sort((a, b) => a.startTime.localeCompare(b.startTime));

  const recentResults = [...(resultsQuery.data?.items ?? [])]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  return (
    <div className="space-y-6">
      <Card className="overflow-hidden">
        <div className="bg-dashboard-glow px-6 py-8 lg:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">My Dashboard</p>
          <h2 className="mt-3 text-3xl font-semibold text-slate-950">
            Welcome back, {user?.fullName?.split(" ")[0] ?? "Student"}
          </h2>
          <p className="mt-4 max-w-2xl text-sm leading-7 text-slate-600">
            {student?.className
              ? `Enrolled in ${student.className}.`
              : "Your academic overview is ready below."}{" "}
            A snapshot of your attendance, recent results, and today&apos;s class schedule.
          </p>
        </div>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Attendance rate" value={stats.attendancePct} icon={ClipboardCheck} />
        <StatCard label="Total exams" value={String(stats.totalExams)} icon={BookOpen} />
        <StatCard label="Latest grade" value={stats.latestGrade} icon={GraduationCap} accent />
        <StatCard label={`Classes today (${todayName})`} value={String(stats.todayCount)} icon={CalendarDays} />
      </div>

      <NotificationsSummaryCard
        title="Recent notifications"
        description="Important updates about your attendance, exams, fees, and results."
        href="/student/notifications"
        emptyMessage="No notifications have been delivered yet."
      />

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Today — {todayName}</p>
          <h3 className="mt-2 text-lg font-semibold text-slate-950">Schedule</h3>
          {timetableQuery.isLoading ? (
            <InlineLoader />
          ) : todayEntries.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">No classes scheduled for today.</p>
          ) : (
            <div className="mt-4 space-y-2.5">
              {todayEntries.map((entry) => (
                <div
                  key={entry.id}
                  className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 transition-colors hover:bg-slate-100/60"
                >
                  <div>
                    <p className="text-sm font-medium text-slate-900">{entry.subjectName}</p>
                    <p className="text-xs text-slate-500">{entry.teacherName}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium text-slate-700">
                      {entry.startTime} – {entry.endTime}
                    </p>
                    {entry.roomNumber ? (
                      <p className="text-xs text-slate-400">{entry.roomNumber}</p>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card className="p-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Academic Performance</p>
          <h3 className="mt-2 text-lg font-semibold text-slate-950">Recent Results</h3>
          {resultsQuery.isLoading ? (
            <InlineLoader />
          ) : recentResults.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">No results recorded yet.</p>
          ) : (
            <div className="mt-4 space-y-2.5">
              {recentResults.map((result) => (
                <div
                  key={result.id}
                  className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 transition-colors hover:bg-slate-100/60"
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
      </div>
    </div>
  );
}
