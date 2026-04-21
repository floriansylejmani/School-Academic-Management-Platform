"use client";

import { useMemo, useState } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useStudentProfile } from "@/features/profile/hooks/use-profile";
import { useStudentResults } from "@/features/student-portal/hooks/use-student-portal";
import type { Result } from "@/features/results/types/results.types";

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

const columns: DataTableColumn<Result>[] = [
  {
    key: "exam",
    header: "Exam",
    render: (r) => <span className="font-semibold text-slate-900">{r.examTitle}</span>
  },
  {
    key: "subject",
    header: "Subject",
    render: (r) => r.subjectName
  },
  {
    key: "marks",
    header: "Marks",
    render: (r) => (
      <span className="font-medium text-slate-700">
        {r.marksObtained} / {r.totalMarks}
      </span>
    )
  },
  {
    key: "grade",
    header: "Grade",
    render: (r) => (
      <span
        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${
          GRADE_COLORS[r.grade] ?? "text-slate-600 bg-slate-100"
        }`}
      >
        {r.grade}
      </span>
    )
  },
  {
    key: "remarks",
    header: "Remarks",
    render: (r) =>
      r.remarks ? (
        <span className="text-slate-600">{r.remarks}</span>
      ) : (
        <span className="text-slate-400">-</span>
      )
  }
];

export function StudentResultsClient() {
  const profileQuery = useStudentProfile();
  const studentId = profileQuery.data?.id;
  const resultsQuery = useStudentResults(studentId);

  const [filterSubject, setFilterSubject] = useState("");

  const { filtered, subjects, averagePct, bestGrade } = useMemo(() => {
    const all = resultsQuery.data?.items ?? [];

    const uniqueSubjects = Array.from(new Set(all.map((r) => r.subjectName))).sort();

    const filt = filterSubject
      ? all.filter((r) => r.subjectName === filterSubject)
      : all;

    const sorted = [...filt].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );

    const totalPct =
      all.length > 0
        ? Math.round(
            all.reduce((sum, r) => sum + (r.totalMarks > 0 ? (r.marksObtained / r.totalMarks) * 100 : 0), 0) /
              all.length
          )
        : 0;

    const gradeOrder = ["A+", "A", "B+", "B", "C+", "C", "D", "F"];
    const best = all.reduce<string | null>((acc, r) => {
      if (!acc) return r.grade;
      return gradeOrder.indexOf(r.grade) < gradeOrder.indexOf(acc) ? r.grade : acc;
    }, null);

    return {
      filtered: sorted,
      subjects: uniqueSubjects,
      averagePct: totalPct,
      bestGrade: best ?? "-"
    };
  }, [resultsQuery.data, filterSubject]);

  if (profileQuery.isLoading || resultsQuery.isLoading) {
    return <LoadingState title="Loading results..." description="Fetching your exam results." />;
  }

  if (profileQuery.isError) {
    return (
      <EmptyState
        title="Profile unavailable"
        description="Your student profile could not be loaded."
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academic Record"
        title="My Results"
        description="All your exam results, grades, and subject-wise performance across the academic year."
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Average score</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">{averagePct}%</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Best grade</p>
          <p
            className={`mt-3 text-3xl font-semibold ${
              GRADE_COLORS[bestGrade]?.split(" ")[0] ?? "text-slate-950"
            }`}
          >
            {bestGrade}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Exams taken</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {resultsQuery.data?.items.length ?? 0}
          </p>
        </Card>
      </div>

      <div className="flex flex-wrap gap-3">
        <Select
          value={filterSubject}
          onChange={(e) => setFilterSubject(e.target.value)}
          placeholder="All subjects"
          className="w-48"
        >
          {subjects.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </Select>
        {filterSubject ? (
          <button
            type="button"
            onClick={() => setFilterSubject("")}
            className="text-sm text-brand-600 hover:underline"
          >
            Clear
          </button>
        ) : null}
      </div>

      {filtered.length === 0 ? (
        <EmptyState
          title="No results found"
          description={
            filterSubject
              ? "No results match the selected subject."
              : "No exam results have been recorded for you yet."
          }
        />
      ) : (
        <DataTable
          columns={columns}
          rows={filtered}
          getRowKey={(r) => r.id}
        />
      )}
    </div>
  );
}
