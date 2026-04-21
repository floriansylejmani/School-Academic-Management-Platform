"use client";

import { useMemo, useState } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Input } from "@/components/ui/input";
import { useStudentProfile } from "@/features/profile/hooks/use-profile";
import { useStudentAttendance } from "@/features/student-portal/hooks/use-student-portal";
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
      r.remarks ? (
        <span className="text-slate-600">{r.remarks}</span>
      ) : (
        <span className="text-slate-400">-</span>
      )
  }
];

export function StudentAttendanceClient() {
  const profileQuery = useStudentProfile();
  const studentId = profileQuery.data?.id;
  const attendanceQuery = useStudentAttendance(studentId);

  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

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
    const excused = all.filter((r) => r.status === "Excused").length;
    const total = all.length;
    const pct =
      total > 0 ? Math.round(((present + late) / total) * 100) : 0;

    return { records: sorted, summary: { present, late, absent, excused, total, pct } };
  }, [attendanceQuery.data, dateFrom, dateTo]);

  if (profileQuery.isLoading || attendanceQuery.isLoading) {
    return <LoadingState title="Loading attendance..." description="Fetching your attendance history." />;
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
        eyebrow="Attendance"
        title="My Attendance"
        description="Your full attendance history with subject-wise breakdown and overall attendance rate."
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
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

      <div className="flex flex-wrap gap-3">
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

      {records.length === 0 ? (
        <EmptyState
          title="No attendance records"
          description={
            dateFrom || dateTo
              ? "No records found in the selected date range."
              : "No attendance has been recorded for you yet."
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
