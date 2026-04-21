"use client";

import { useMemo, useState } from "react";
import { CircleAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useAttendance } from "@/features/attendance/hooks/use-attendance";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { useFees } from "@/features/fees/hooks/use-fees";
import { useResults } from "@/features/results/hooks/use-results";
import { useStudents } from "@/features/students/hooks/use-students";
import { reportsService, type ReportPdfType } from "@/services/reports.service";
import { getApiErrorMessageAsync } from "@/utils/api";
import type { AttendanceRecord } from "@/features/attendance/types/attendance.types";
import type { Fee } from "@/features/fees/types/fees.types";
import type { Result } from "@/features/results/types/results.types";
import type { Student } from "@/features/students/types/student.types";

type ReportView = "reportCard" | "attendance" | "fees";

function isWithinDate(value: string, from: string, to: string) {
  const date = value.slice(0, 10);
  return (!from || date >= from) && (!to || date <= to);
}

function getAttendancePercentage(records: AttendanceRecord[]) {
  if (records.length === 0) {
    return 0;
  }

  return (records.filter((record) => record.status === "Present").length / records.length) * 100;
}

function getAverageScore(results: Result[]) {
  const scored = results.filter((result) => result.totalMarks > 0);
  if (scored.length === 0) {
    return 0;
  }

  return scored.reduce((sum, result) => sum + (result.marksObtained / result.totalMarks) * 100, 0) / scored.length;
}

function getPaidAmount(fee: Fee) {
  return fee.payments.reduce((sum, payment) => sum + payment.amountPaid, 0);
}

function money(value: number) {
  return `$${value.toFixed(2)}`;
}

function StudentReportCards({
  students,
  attendance,
  results,
  fees
}: {
  students: Student[];
  attendance: AttendanceRecord[];
  results: Result[];
  fees: Fee[];
}) {
  if (students.length === 0) {
    return (
      <EmptyState
        title="No students found"
        description="Adjust the filters to build report cards for matching students."
      />
    );
  }

  return (
    <div className="grid gap-4 xl:grid-cols-2">
      {students.map((student) => {
        const studentAttendance = attendance.filter((record) => record.studentId === student.id);
        const studentResults = results.filter((result) => result.studentId === student.id);
        const studentFees = fees.filter((fee) => fee.studentId === student.id);
        const outstanding = studentFees.reduce((sum, fee) => sum + Math.max(fee.amount - getPaidAmount(fee), 0), 0);

        return (
          <Card key={student.id} className="p-6">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xl font-semibold text-slate-950">{student.fullName}</p>
                <p className="mt-1 text-sm text-slate-500">
                  {student.studentCode} / {student.className ?? "No class assigned"}
                </p>
              </div>
              <Badge>{student.email}</Badge>
            </div>

            <div className="mt-5 grid gap-3 sm:grid-cols-3">
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Attendance</p>
                <p className="mt-2 text-2xl font-semibold text-slate-950">
                  {getAttendancePercentage(studentAttendance).toFixed(1)}%
                </p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Average score</p>
                <p className="mt-2 text-2xl font-semibold text-slate-950">
                  {getAverageScore(studentResults).toFixed(1)}%
                </p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Outstanding</p>
                <p className="mt-2 text-2xl font-semibold text-slate-950">{money(outstanding)}</p>
              </div>
            </div>

            <div className="mt-5 space-y-3">
              {studentResults.slice(0, 4).map((result) => (
                <div
                  key={result.id}
                  className="flex items-center justify-between gap-3 rounded-2xl bg-slate-50 px-4 py-3"
                >
                  <div>
                    <p className="font-medium text-slate-900">{result.examTitle}</p>
                    <p className="mt-1 text-sm text-slate-500">{result.subjectName}</p>
                  </div>
                  <Badge>{result.grade}</Badge>
                </div>
              ))}
              {studentResults.length === 0 ? (
                <p className="text-sm text-slate-500">No results recorded for this student.</p>
              ) : null}
            </div>
          </Card>
        );
      })}
    </div>
  );
}

function AttendanceSummary({ records }: { records: AttendanceRecord[] }) {
  if (records.length === 0) {
    return (
      <EmptyState
        title="No attendance records found"
        description="Adjust class, student, or date filters to review attendance."
      />
    );
  }

  const statusCounts = records.reduce<Record<string, number>>((acc, record) => {
    acc[record.status] = (acc[record.status] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-4">
        {["Present", "Absent", "Late", "Excused"].map((status) => (
          <Card key={status} className="p-5">
            <p className="text-sm text-slate-500">{status}</p>
            <p className="mt-2 text-3xl font-semibold text-slate-950">{statusCounts[status] ?? 0}</p>
          </Card>
        ))}
      </div>
      <Card className="overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200">
            <thead className="bg-slate-50">
              <tr>
                {["Student", "Class", "Subject", "Date", "Status"].map((header) => (
                  <th
                    key={header}
                    className="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-[0.22em] text-slate-500"
                  >
                    {header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 bg-white">
              {records.slice(0, 50).map((record) => (
                <tr key={record.id} className="transition-colors duration-75 hover:bg-slate-50/70">
                  <td className="px-5 py-3.5 text-sm text-slate-700">{record.studentName}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{record.className}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{record.subjectName}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{record.date}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">
                    <Badge>{record.status}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {records.length > 50 ? (
          <div className="border-t border-slate-100 px-5 py-3 text-xs text-slate-400">
            Showing 50 of {records.length} records
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function FeeSummary({ fees }: { fees: Fee[] }) {
  if (fees.length === 0) {
    return (
      <EmptyState
        title="No fees found"
        description="Adjust class, student, or date filters to review fee summaries."
      />
    );
  }

  const totalBilled = fees.reduce((sum, fee) => sum + fee.amount, 0);
  const totalPaid = fees.reduce((sum, fee) => sum + getPaidAmount(fee), 0);
  const totalOutstanding = totalBilled - totalPaid;

  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Total billed</p>
          <p className="mt-2 text-3xl font-semibold text-slate-950">{money(totalBilled)}</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Total paid</p>
          <p className="mt-2 text-3xl font-semibold text-slate-950">{money(totalPaid)}</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Outstanding</p>
          <p className="mt-2 text-3xl font-semibold text-slate-950">{money(totalOutstanding)}</p>
        </Card>
      </div>
      <Card className="overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-200">
            <thead className="bg-slate-50">
              <tr>
                {["Student", "Fee", "Billed", "Paid", "Due date", "Status"].map((header) => (
                  <th
                    key={header}
                    className="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-[0.22em] text-slate-500"
                  >
                    {header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 bg-white">
              {fees.slice(0, 50).map((fee) => (
                <tr key={fee.id} className="transition-colors duration-75 hover:bg-slate-50/70">
                  <td className="px-5 py-3.5 text-sm text-slate-700">{fee.studentName}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{fee.feeType}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{money(fee.amount)}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{money(getPaidAmount(fee))}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">{fee.dueDate}</td>
                  <td className="px-5 py-3.5 text-sm text-slate-700">
                    <Badge>{fee.status}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {fees.length > 50 ? (
          <div className="border-t border-slate-100 px-5 py-3 text-xs text-slate-400">
            Showing 50 of {fees.length} records
          </div>
        ) : null}
      </Card>
    </div>
  );
}

export function ReportsAdminClient() {
  const studentsQuery = useStudents();
  const classesQuery = useClasses();
  const attendanceQuery = useAttendance();
  const resultsQuery = useResults();
  const feesQuery = useFees();
  const [view, setView] = useState<ReportView>("reportCard");
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);
  const [filters, setFilters] = useState({
    classId: "",
    studentId: "",
    dateFrom: "",
    dateTo: ""
  });

  const queries = [studentsQuery, classesQuery, attendanceQuery, resultsQuery, feesQuery];

  const students = useMemo(() => {
    return (studentsQuery.data?.items ?? []).filter((student) => {
      return (
        (!filters.classId || student.classId === filters.classId) &&
        (!filters.studentId || student.id === filters.studentId)
      );
    });
  }, [filters.classId, filters.studentId, studentsQuery.data?.items]);

  const selectedStudentIds = useMemo(() => new Set(students.map((student) => student.id)), [students]);

  const attendance = useMemo(() => {
    return (attendanceQuery.data?.items ?? []).filter((record) => {
      return selectedStudentIds.has(record.studentId) && isWithinDate(record.date, filters.dateFrom, filters.dateTo);
    });
  }, [attendanceQuery.data?.items, filters.dateFrom, filters.dateTo, selectedStudentIds]);

  const results = useMemo(() => {
    return (resultsQuery.data?.items ?? []).filter((result) => selectedStudentIds.has(result.studentId));
  }, [resultsQuery.data?.items, selectedStudentIds]);

  const fees = useMemo(() => {
    return (feesQuery.data?.items ?? []).filter((fee) => {
      return selectedStudentIds.has(fee.studentId) && isWithinDate(fee.dueDate, filters.dateFrom, filters.dateTo);
    });
  }, [feesQuery.data?.items, filters.dateFrom, filters.dateTo, selectedStudentIds]);

  const reportType: ReportPdfType = view === "reportCard" ? "students" : view;

  async function handleDownloadPdf() {
    setDownloadError(null);

    if (filters.dateFrom && filters.dateTo && filters.dateFrom > filters.dateTo) {
      setDownloadError("The start date must be earlier than or equal to the end date.");
      return;
    }

    setIsDownloading(true);

    try {
      const { blob, fileName } = await reportsService.downloadPdf(reportType, {
        classId: filters.classId || undefined,
        studentId: filters.studentId || undefined,
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined
      });

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.append(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      setDownloadError(
        await getApiErrorMessageAsync(error, "The PDF report could not be generated right now. Try again in a moment.")
      );
    } finally {
      setIsDownloading(false);
    }
  }

  if (queries.some((query) => query.isLoading)) {
    return <LoadingState title="Loading reports..." description="Gathering students, attendance, results, and fee data." />;
  }

  if (queries.some((query) => query.isError)) {
    return (
      <EmptyState
        title="Unable to load reports"
        description="Report data could not be loaded right now. Check the backend connection and try again."
        action={<Button onClick={() => queries.forEach((query) => void query.refetch())}>Retry</Button>}
      />
    );
  }

  const hasActiveFilters = filters.classId || filters.studentId || filters.dateFrom || filters.dateTo;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Reporting"
        title="Academic Reports"
        description="Generate student report cards, attendance summaries, and fee statements with flexible class, student, and date filters."
        actionLabel={isDownloading ? "Downloading PDF..." : "Download PDF"}
        onAction={() => void handleDownloadPdf()}
        actionDisabled={isDownloading}
      />

      <Card className="p-5">
        <div className="mb-4 flex items-center justify-between gap-4">
          <div>
            <p className="text-sm font-semibold text-slate-900">Filters</p>
            <p className="mt-0.5 text-xs text-slate-500">Narrow the report by class, student, or date range.</p>
          </div>
          {hasActiveFilters ? (
            <Button
              variant="ghost"
              className="h-8 px-3 text-xs"
              onClick={() => setFilters({ classId: "", studentId: "", dateFrom: "", dateTo: "" })}
            >
              Clear filters
            </Button>
          ) : null}
        </div>

        <div className="flex flex-wrap gap-3">
          <Select
            value={view}
            onChange={(event) => setView(event.target.value as ReportView)}
            className="w-56"
          >
            <option value="reportCard">Student report cards</option>
            <option value="attendance">Attendance summary</option>
            <option value="fees">Fee summary</option>
          </Select>
          <Select
            value={filters.classId}
            onChange={(event) =>
              setFilters((current) => ({ ...current, classId: event.target.value, studentId: "" }))
            }
            placeholder="All classes"
            className="w-52"
          >
            {(classesQuery.data?.items ?? []).map((academicClass) => (
              <option key={academicClass.id} value={academicClass.id}>
                {academicClass.name} {academicClass.section}
              </option>
            ))}
          </Select>
          <Select
            value={filters.studentId}
            onChange={(event) => setFilters((current) => ({ ...current, studentId: event.target.value }))}
            placeholder="All students"
            className="w-56"
          >
            {(studentsQuery.data?.items ?? [])
              .filter((student) => !filters.classId || student.classId === filters.classId)
              .map((student) => (
                <option key={student.id} value={student.id}>
                  {student.fullName}
                </option>
              ))}
          </Select>
          <div className="flex items-center gap-2">
            <Input
              type="date"
              value={filters.dateFrom}
              onChange={(event) => setFilters((current) => ({ ...current, dateFrom: event.target.value }))}
              className="w-40"
              aria-label="Date from"
            />
            <span className="text-sm text-slate-400">–</span>
            <Input
              type="date"
              value={filters.dateTo}
              onChange={(event) => setFilters((current) => ({ ...current, dateTo: event.target.value }))}
              className="w-40"
              aria-label="Date to"
            />
          </div>
        </div>

        {downloadError ? (
          <div className="mt-4 flex items-start gap-3 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{downloadError}</span>
          </div>
        ) : null}
      </Card>

      {view === "reportCard" ? (
        <StudentReportCards students={students} attendance={attendance} results={results} fees={fees} />
      ) : null}
      {view === "attendance" ? <AttendanceSummary records={attendance} /> : null}
      {view === "fees" ? <FeeSummary fees={fees} /> : null}
    </div>
  );
}
