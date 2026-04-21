"use client";

import { useMemo, useState } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { useAuthStore } from "@/store/auth.store";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import { useChildResults } from "@/features/parent-portal/hooks/use-parent-portal";
import { Select } from "@/components/ui/select";
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
      r.remarks ? <span className="text-slate-600">{r.remarks}</span> : <span className="text-slate-400">-</span>
  }
];

export function ParentResultsClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];

  const [filterSubject, setFilterSubject] = useState("");
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);

  const resultsQuery = useChildResults(activeChildId);

  const { filtered, subjects, averagePct } = useMemo(() => {
    const all = resultsQuery.data?.items ?? [];
    const uniqueSubjects = Array.from(new Set(all.map((r) => r.subjectName))).sort();
    const filt = filterSubject ? all.filter((r) => r.subjectName === filterSubject) : all;
    const sorted = [...filt].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
    const avgPct =
      all.length > 0
        ? Math.round(
            all.reduce((sum, r) => sum + (r.totalMarks > 0 ? (r.marksObtained / r.totalMarks) * 100 : 0), 0) / all.length
          )
        : 0;
    return { filtered: sorted, subjects: uniqueSubjects, averagePct: avgPct };
  }, [resultsQuery.data, filterSubject]);

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading..." description="Fetching child profile." />;
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
        description="No student is linked to your parent account."
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academic Results"
        title="Exam Results"
        description={`Assessment results and grades for ${activeChild?.fullName ?? "your child"} across all subjects.`}
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Average score</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">{averagePct}%</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Exams taken</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {resultsQuery.data?.items.length ?? 0}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Subjects covered</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">{subjects.length}</p>
        </Card>
      </div>

      <div className="flex flex-wrap gap-3">
        <ParentChildSwitcher
          students={children}
          value={activeChildId}
          onChange={(childId) => {
            setSelectedChildId(childId);
            setFilterSubject("");
          }}
          className="w-56"
        />

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

      {resultsQuery.isLoading ? (
        <LoadingState title="Loading results..." description="Fetching exam data." />
      ) : resultsQuery.isError ? (
        <EmptyState
          title="Unable to load results"
          description="Result data could not be loaded for the selected child."
        />
      ) : filtered.length === 0 ? (
        <EmptyState
          title="No results found"
          description={
            filterSubject ? "No results match the selected subject." : "No results have been recorded yet."
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
