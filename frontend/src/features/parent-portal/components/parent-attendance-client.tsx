"use client";

import { useMemo, useState } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { useAuthStore } from "@/store/auth.store";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import { useChildAttendance } from "@/features/parent-portal/hooks/use-parent-portal";
import type { AttendanceRecord, AttendanceStatus } from "@/features/attendance/types/attendance.types";

const STATUS_COLORS: Record<AttendanceStatus, string> = {
  Present: "text-emerald-700 bg-emerald-50",
  Late: "text-amber-700 bg-amber-50",
  Absent: "text-rose-700 bg-rose-50",
  Excused: "text-blue-700 bg-blue-50"
};

const columns: DataTableColumn<AttendanceRecord>[] = [
  {
    key: "date",
    header: "Date",
    render: (r) => <span className="font-medium text-slate-700">{r.date}</span>
  },
  {
    key: "subject",
    header: "Subject",
    render: (r) => r.subjectName
  },
  {
    key: "teacher",
    header: "Teacher",
    render: (r) => r.teacherName
  },
  {
    key: "status",
    header: "Status",
    render: (r) => (
      <span
        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${STATUS_COLORS[r.status]}`}
      >
        {r.status}
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

export function ParentAttendanceClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);

  const attendanceQuery = useChildAttendance(activeChildId);

  const { records, summary } = useMemo(() => {
    const all = attendanceQuery.data?.items ?? [];

    const filtered = all.filter((r) => {
      if (dateFrom && r.date < dateFrom) return false;
      if (dateTo && r.date > dateTo) return false;
      return true;
    });

    const sorted = [...filtered].sort(
      (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()
    );

    const present = all.filter((r) => r.status === "Present").length;
    const late = all.filter((r) => r.status === "Late").length;
    const absent = all.filter((r) => r.status === "Absent").length;
    const pct = all.length > 0 ? Math.round(((present + late) / all.length) * 100) : 0;

    return { records: sorted, summary: { present, late, absent, pct } };
  }, [attendanceQuery.data, dateFrom, dateTo]);

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
        eyebrow="Attendance"
        title="Attendance Record"
        description={`Attendance history for ${activeChild?.fullName ?? "your child"} — present, late, excused, and absent entries.`}
      />

      <div className="flex flex-wrap gap-3">
        <ParentChildSwitcher students={children} value={activeChildId} onChange={setSelectedChildId} className="w-56" />

        <div className="flex items-center gap-2">
          <span className="text-sm text-slate-500">From</span>
          <Input
            type="date"
            value={dateFrom}
            onChange={(e) => setDateFrom(e.target.value)}
            className="w-40"
          />
        </div>
        <div className="flex items-center gap-2">
          <span className="text-sm text-slate-500">To</span>
          <Input
            type="date"
            value={dateTo}
            onChange={(e) => setDateTo(e.target.value)}
            className="w-40"
          />
        </div>
        {(dateFrom || dateTo) ? (
          <button
            type="button"
            onClick={() => { setDateFrom(""); setDateTo(""); }}
            className="text-sm text-brand-600 hover:underline"
          >
            Clear
          </button>
        ) : null}
      </div>

      <div className="grid gap-4 sm:grid-cols-4">
        {[
          { label: "Attendance rate", value: `${summary.pct}%`, color: "text-emerald-600" },
          { label: "Present", value: String(summary.present), color: "text-emerald-600" },
          { label: "Late", value: String(summary.late), color: "text-amber-600" },
          { label: "Absent", value: String(summary.absent), color: "text-rose-600" }
        ].map((s) => (
          <Card key={s.label} className="p-5">
            <p className="text-sm text-slate-500">{s.label}</p>
            <p className={`mt-3 text-3xl font-semibold ${s.color}`}>{s.value}</p>
          </Card>
        ))}
      </div>

      {attendanceQuery.isLoading ? (
        <LoadingState title="Loading attendance..." description="Fetching records." />
      ) : attendanceQuery.isError ? (
        <EmptyState
          title="Unable to load attendance"
          description="Attendance records could not be loaded for the selected child."
        />
      ) : records.length === 0 ? (
        <EmptyState
          title="No attendance records"
          description={
            dateFrom || dateTo
              ? "No records in the selected date range."
              : "No attendance has been recorded yet."
          }
        />
      ) : (
        <DataTable
          columns={columns}
          rows={records}
          getRowKey={(r) => r.id}
        />
      )}
    </div>
  );
}
