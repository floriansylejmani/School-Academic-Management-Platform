"use client";

import {
  BookOpenCheck,
  CalendarDays,
  ClipboardCheck,
  GraduationCap,
  Users
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DemoEmptyState, RoleDemoCTA } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { useAttendance } from "@/features/attendance/hooks/use-attendance";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { useExams } from "@/features/exams/hooks/use-exams";
import { useTeacherProfile } from "@/features/profile/hooks/use-profile";
import { useResults } from "@/features/results/hooks/use-results";
import { useStudents } from "@/features/students/hooks/use-students";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";
import { useTimetable } from "@/features/timetable/hooks/use-timetable";

const DAYS_OF_WEEK = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday"
] as const;

function formatNumber(value: number) {
  return new Intl.NumberFormat("en-US").format(value);
}

export function TeacherDashboardClient() {
  const profileQuery = useTeacherProfile();
  const classesQuery = useClasses();
  const studentsQuery = useStudents();
  const subjectsQuery = useSubjects();
  const timetableQuery = useTimetable();
  const attendanceQuery = useAttendance();
  const examsQuery = useExams();
  const resultsQuery = useResults();

  const queries = [
    profileQuery,
    classesQuery,
    studentsQuery,
    subjectsQuery,
    timetableQuery,
    attendanceQuery,
    examsQuery,
    resultsQuery
  ];

  if (queries.some((query) => query.isLoading)) {
    return (
      <LoadingState
        title="Loading teacher dashboard..."
        description="Collecting your classes, timetable, attendance, and assessment data."
      />
    );
  }

  if (queries.some((query) => query.isError)) {
    return (
      <DemoEmptyState
        feature="Teacher Workspace"
        description="One or more teacher data sources could not be loaded. Check the backend connection and try again."
        demoAction={
          <Button
            onClick={() => {
              void profileQuery.refetch();
              void classesQuery.refetch();
              void studentsQuery.refetch();
              void subjectsQuery.refetch();
              void timetableQuery.refetch();
              void attendanceQuery.refetch();
              void examsQuery.refetch();
              void resultsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  const teacher = profileQuery.data;
  const classes = classesQuery.data?.items ?? [];
  const students = studentsQuery.data?.items ?? [];
  const subjects = subjectsQuery.data?.items ?? [];
  const timetableEntries = timetableQuery.data?.items ?? [];
  const attendanceRecords = attendanceQuery.data?.items ?? [];
  const exams = examsQuery.data?.items ?? [];
  const results = resultsQuery.data?.items ?? [];

  const todayName = DAYS_OF_WEEK[new Date().getDay()];

  const summary = {
    todayLessons: timetableEntries
      .filter((entry) => entry.dayOfWeek === todayName)
      .sort((a, b) => a.startTime.localeCompare(b.startTime)),
    upcomingExams: exams
      .filter((exam) => new Date(exam.examDate) >= new Date())
      .sort((a, b) => new Date(a.examDate).getTime() - new Date(b.examDate).getTime()),
    recentResults: [...results]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5),
    recentAttendance: [...attendanceRecords]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5)
  };

  const metrics = [
    { label: "Assigned classes", value: formatNumber(classes.length), icon: GraduationCap },
    { label: "Students reached", value: formatNumber(students.length), icon: Users },
    { label: "Subjects taught", value: formatNumber(subjects.length), icon: BookOpenCheck },
    { label: `Lessons today (${todayName})`, value: formatNumber(summary.todayLessons.length), icon: CalendarDays },
    { label: "Attendance records", value: formatNumber(attendanceRecords.length), icon: ClipboardCheck },
    { label: "Results recorded", value: formatNumber(results.length), icon: BookOpenCheck }
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="My Workspace"
        title={`Welcome back, ${teacher?.fullName ?? "Teacher"}`}
        description="Your daily schedule, class overview, attendance records, and upcoming assessments — all in one place."
      />

      {classes.length === 0 && timetableEntries.length === 0 ? (
        <div className="space-y-6">
          <DemoEmptyState
            feature="Teaching Management"
            description="This demo includes sample teachers with assigned classes, subjects, and timetables. Try logging in as a teacher to explore attendance marking, grade entry, and class management features."
            demoAction={<RoleDemoCTA role="teacher" />}
          />
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {metrics.map((metric) => {
          const Icon = metric.icon;

          return (
            <Card key={metric.label} className="p-5">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm text-slate-500">{metric.label}</p>
                  <p className="mt-3 text-3xl font-semibold tabular-nums text-slate-950">{metric.value}</p>
                </div>
                <div className="shrink-0 rounded-2xl bg-brand-50 p-3 text-brand-700">
                  <Icon className="h-5 w-5" />
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
        <Card className="p-6">
          <p className="text-sm font-semibold text-slate-950">Today&apos;s lessons</p>
          <p className="mt-1 text-sm text-slate-500">Your teaching schedule for {todayName}.</p>
          <div className="mt-5 space-y-2.5">
            {summary.todayLessons.length === 0 ? (
              <p className="py-4 text-center text-sm text-slate-500">No lessons are scheduled for today.</p>
            ) : (
              summary.todayLessons.map((entry) => (
                <div key={entry.id} className="rounded-2xl bg-slate-50 px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-slate-900">{entry.subjectName}</p>
                      <p className="mt-1 text-xs text-slate-500">{entry.className}</p>
                    </div>
                    <div className="text-right text-sm text-slate-600">
                      <p>
                        {entry.startTime} - {entry.endTime}
                      </p>
                      {entry.roomNumber ? <p className="mt-1 text-xs text-slate-400">{entry.roomNumber}</p> : null}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>

        <Card className="p-6">
          <p className="text-sm font-semibold text-slate-950">Upcoming exams</p>
          <p className="mt-1 text-sm text-slate-500">Assessments scheduled for your assigned classes and subjects.</p>
          <div className="mt-5 space-y-2.5">
            {summary.upcomingExams.length === 0 ? (
              <p className="py-4 text-center text-sm text-slate-500">No upcoming exams are scheduled.</p>
            ) : (
              summary.upcomingExams.slice(0, 5).map((exam) => (
                <div key={exam.id} className="rounded-2xl bg-slate-50 px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-slate-900">{exam.title}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        {exam.className} - {exam.subjectName}
                      </p>
                    </div>
                    <p className="text-sm text-slate-600">{exam.examDate}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card className="p-6">
          <p className="text-sm font-semibold text-slate-950">Recent attendance</p>
          <p className="mt-1 text-sm text-slate-500">Latest attendance updates you recorded.</p>
          <div className="mt-5 space-y-2.5">
            {summary.recentAttendance.length === 0 ? (
              <p className="py-4 text-center text-sm text-slate-500">No attendance records have been saved yet.</p>
            ) : (
              summary.recentAttendance.map((record) => (
                <div key={record.id} className="rounded-2xl bg-slate-50 px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-slate-900">{record.studentName}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        {record.className} - {record.subjectName}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-slate-700">{record.status}</p>
                      <p className="mt-1 text-xs text-slate-400">{record.date}</p>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>

        <Card className="p-6">
          <p className="text-sm font-semibold text-slate-950">Recent results</p>
          <p className="mt-1 text-sm text-slate-500">Most recently recorded marks across your exams.</p>
          <div className="mt-5 space-y-2.5">
            {summary.recentResults.length === 0 ? (
              <p className="py-4 text-center text-sm text-slate-500">No results have been recorded yet.</p>
            ) : (
              summary.recentResults.map((result) => (
                <div key={result.id} className="rounded-2xl bg-slate-50 px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-slate-900">{result.studentName}</p>
                      <p className="mt-1 text-xs text-slate-500">
                        {result.examTitle} - {result.subjectName}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium text-slate-700">
                        {result.marksObtained}/{result.totalMarks}
                      </p>
                      <p className="mt-1 text-xs text-slate-400">{result.grade}</p>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>
    </div>
  );
}
