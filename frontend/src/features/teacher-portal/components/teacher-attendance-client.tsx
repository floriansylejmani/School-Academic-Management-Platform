"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { z } from "zod";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useAttendance, useCreateAttendance, useUpdateAttendance } from "@/features/attendance/hooks/use-attendance";
import type {
  AttendanceRecord,
  AttendanceStatus,
  CreateAttendanceDto,
  UpdateAttendanceDto
} from "@/features/attendance/types/attendance.types";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { useTeacherProfile } from "@/features/profile/hooks/use-profile";
import { useStudents } from "@/features/students/hooks/use-students";
import { useTimetable } from "@/features/timetable/hooks/use-timetable";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";

const attendanceSchema = z.object({
  classId: z.string().min(1, "Class is required"),
  subjectId: z.string().min(1, "Subject is required"),
  date: z.string().min(1, "Date is required"),
  rows: z.array(
    z.object({
      attendanceId: z.string().optional(),
      studentId: z.string(),
      studentName: z.string(),
      status: z.enum(["Present", "Absent", "Late", "Excused"]),
      remarks: z.string().max(250, "Maximum 250 characters").optional().or(z.literal(""))
    })
  )
});

type AttendanceFormValues = z.infer<typeof attendanceSchema>;

const statusOptions: AttendanceStatus[] = ["Present", "Absent", "Late", "Excused"];

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function TeacherAttendanceClient() {
  const toast = useToast();
  const teacherQuery = useTeacherProfile();
  const classesQuery = useClasses();
  const studentsQuery = useStudents();
  const timetableQuery = useTimetable();
  const attendanceQuery = useAttendance();
  const createAttendance = useCreateAttendance({ silentSuccess: true, skipInvalidate: true });
  const updateAttendance = useUpdateAttendance({ silentSuccess: true, skipInvalidate: true });
  const [isSaving, setIsSaving] = useState(false);
  const [historyFilters, setHistoryFilters] = useState({
    classId: "",
    studentId: "",
    startDate: "",
    endDate: ""
  });

  const form = useForm<AttendanceFormValues>({
    resolver: zodResolver(attendanceSchema),
    defaultValues: {
      classId: "",
      subjectId: "",
      date: todayIso(),
      rows: []
    }
  });

  const { control, register, handleSubmit, setValue, watch, formState } = form;
  const { fields, replace } = useFieldArray({
    control,
    name: "rows"
  });

  const selectedClassId = watch("classId");
  const selectedSubjectId = watch("subjectId");
  const selectedDate = watch("date");

  const teacherId = teacherQuery.data?.id;
  const classes = classesQuery.data?.items ?? [];
  const students = studentsQuery.data?.items ?? [];
  const timetableEntries = timetableQuery.data?.items ?? [];
  const attendanceRecords = attendanceQuery.data?.items ?? [];

  const availableClassIds = useMemo(
    () => new Set([...timetableEntries.map((entry) => entry.classId), ...attendanceRecords.map((record) => record.classId)]),
    [attendanceRecords, timetableEntries]
  );

  const availableClasses = useMemo(
    () => classes.filter((item) => availableClassIds.has(item.id)),
    [availableClassIds, classes]
  );

  const availableSubjects = useMemo(() => {
    if (!selectedClassId) {
      return [];
    }

    const map = new Map<string, { id: string; name: string }>();

    timetableEntries
      .filter((entry) => entry.classId === selectedClassId)
      .forEach((entry) => map.set(entry.subjectId, { id: entry.subjectId, name: entry.subjectName }));

    attendanceRecords
      .filter((record) => record.classId === selectedClassId)
      .forEach((record) => map.set(record.subjectId, { id: record.subjectId, name: record.subjectName }));

    return [...map.values()].sort((a, b) => a.name.localeCompare(b.name));
  }, [attendanceRecords, selectedClassId, timetableEntries]);

  const selectedClass = availableClasses.find((item) => item.id === selectedClassId);

  const classStudents = useMemo(
    () => students.filter((student) => student.classId === selectedClassId),
    [selectedClassId, students]
  );

  const derivedRows = useMemo(() => {
    if (!selectedClassId || !selectedSubjectId) {
      return [];
    }

    return classStudents.map((student) => {
      const existingRecord = attendanceRecords.find(
        (record) =>
          record.studentId === student.id &&
          record.classId === selectedClassId &&
          record.subjectId === selectedSubjectId &&
          record.date === selectedDate
      );

      return {
        attendanceId: existingRecord?.id,
        studentId: student.id,
        studentName: student.fullName,
        status: (existingRecord?.status ?? "Present") as AttendanceStatus,
        remarks: existingRecord?.remarks ?? ""
      };
    });
  }, [attendanceRecords, classStudents, selectedClassId, selectedDate, selectedSubjectId]);

  useEffect(() => {
    replace(derivedRows);
  }, [derivedRows, replace]);

  useEffect(() => {
    if (availableSubjects.length === 0) {
      setValue("subjectId", "");
      return;
    }

    const hasCurrentSubject = availableSubjects.some((subject) => subject.id === selectedSubjectId);
    if (!hasCurrentSubject) {
      setValue("subjectId", availableSubjects[0]?.id ?? "");
    }
  }, [availableSubjects, selectedSubjectId, setValue]);

  const filteredHistory = useMemo(() => {
    return attendanceRecords.filter((record) => {
      const matchesClass = historyFilters.classId ? record.classId === historyFilters.classId : true;
      const matchesStudent = historyFilters.studentId ? record.studentId === historyFilters.studentId : true;
      const matchesStart = historyFilters.startDate ? record.date >= historyFilters.startDate : true;
      const matchesEnd = historyFilters.endDate ? record.date <= historyFilters.endDate : true;
      return matchesClass && matchesStudent && matchesStart && matchesEnd;
    });
  }, [attendanceRecords, historyFilters]);

  const historyColumns: DataTableColumn<AttendanceRecord>[] = [
    {
      key: "student",
      header: "Student",
      render: (record) => (
        <div>
          <p className="font-semibold">{record.studentName}</p>
          <p className="text-slate-500">{record.className}</p>
        </div>
      )
    },
    { key: "subject", header: "Subject", render: (record) => record.subjectName },
    { key: "date", header: "Date", render: (record) => record.date },
    {
      key: "status",
      header: "Status",
      render: (record) => (
        <Badge
          className={
            record.status === "Present"
              ? "bg-emerald-50 text-emerald-700"
              : record.status === "Late"
                ? "bg-amber-50 text-amber-700"
                : record.status === "Excused"
                  ? "bg-blue-50 text-blue-700"
                  : "bg-rose-50 text-rose-700"
          }
        >
          {record.status}
        </Badge>
      )
    }
  ];

  if (
    teacherQuery.isLoading ||
    classesQuery.isLoading ||
    studentsQuery.isLoading ||
    timetableQuery.isLoading ||
    attendanceQuery.isLoading
  ) {
    return (
      <LoadingState
        title="Loading teacher attendance..."
        description="Preparing your classes, students, timetable, and attendance history."
      />
    );
  }

  if (
    teacherQuery.isError ||
    classesQuery.isError ||
    studentsQuery.isError ||
    timetableQuery.isError ||
    attendanceQuery.isError
  ) {
    return (
      <EmptyState
        title="Unable to load attendance workspace"
        description="Teacher attendance data could not be loaded. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void teacherQuery.refetch();
              void classesQuery.refetch();
              void studentsQuery.refetch();
              void timetableQuery.refetch();
              void attendanceQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  if (!teacherId) {
    return (
      <EmptyState
        title="Teacher profile unavailable"
        description="Your teacher profile could not be resolved. Please contact an administrator."
      />
    );
  }

  if (availableClasses.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Attendance"
          title="Attendance Register"
          description="Record attendance for your assigned classes and review the history attached to your account."
        />
        <EmptyState
          title="No attendance classes available"
          description="Your account does not have any timetable-backed class assignments yet, so attendance cannot be recorded."
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Teacher / Attendance"
        title="Attendance"
        description="Record attendance for your assigned lessons and review the full history scoped to your teacher account."
      />

      <Card className="p-6 lg:p-7">
        <form
          className="space-y-6"
          onSubmit={handleSubmit((values) => {
            void (async () => {
              setIsSaving(true);
              try {
                const operations = values.rows.map(async (row) => {
                  const payloadBase = {
                    studentId: row.studentId,
                    classId: values.classId,
                    subjectId: values.subjectId,
                    teacherId,
                    date: values.date,
                    status: row.status,
                    remarks: toNullable(row.remarks)
                  };

                  if (row.attendanceId) {
                    await updateAttendance.mutateAsync({
                      id: row.attendanceId,
                      payload: payloadBase satisfies UpdateAttendanceDto
                    });
                    return;
                  }

                  await createAttendance.mutateAsync(payloadBase satisfies CreateAttendanceDto);
                });

                await Promise.all(operations);
                await attendanceQuery.refetch();
                toast.success("Attendance saved", "Attendance was recorded for the selected class.");
              } catch (error) {
                toast.error("Unable to save attendance", getApiErrorMessage(error));
              } finally {
                setIsSaving(false);
              }
            })();
          })}
        >
          <div className="grid gap-4 lg:grid-cols-3">
            <FormField label="Class" error={formState.errors.classId?.message}>
              <Select {...register("classId")} placeholder="Select class">
                {availableClasses.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} {item.section}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Subject" error={formState.errors.subjectId?.message}>
              <Select {...register("subjectId")} placeholder="Select subject">
                {availableSubjects.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Date" error={formState.errors.date?.message}>
              <Input type="date" {...register("date")} />
            </FormField>
          </div>

          {fields.length === 0 ? (
            <EmptyState
              title="No students ready for attendance"
              description="Choose a class and subject with enrolled students to start recording attendance."
            />
          ) : (
            <div className="overflow-hidden rounded-[28px] border border-slate-200">
              <div className="grid grid-cols-[1.4fr_0.7fr_1fr] gap-4 border-b border-slate-200 bg-slate-50 px-5 py-4 text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">
                <span>Student</span>
                <span>Status</span>
                <span>Remarks</span>
              </div>
              <div className="divide-y divide-slate-200 bg-white">
                {fields.map((field, index) => (
                  <div key={field.id} className="grid grid-cols-[1.4fr_0.7fr_1fr] gap-4 px-5 py-4">
                    <div>
                      <p className="font-semibold text-slate-900">{field.studentName}</p>
                      <p className="mt-1 text-sm text-slate-500">
                        {selectedClass?.name} {selectedClass?.section}
                      </p>
                    </div>
                    <div>
                      <Select {...register(`rows.${index}.status`)}>
                        {statusOptions.map((status) => (
                          <option key={status} value={status}>
                            {status}
                          </option>
                        ))}
                      </Select>
                    </div>
                    <div>
                      <Input placeholder="Optional note" {...register(`rows.${index}.remarks`)} />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="flex justify-end">
            <Button
              type="submit"
              disabled={isSaving || createAttendance.isPending || updateAttendance.isPending || fields.length === 0}
            >
              {isSaving || createAttendance.isPending || updateAttendance.isPending ? "Saving attendance..." : "Save attendance"}
            </Button>
          </div>
        </form>
      </Card>

      <Card className="p-6 lg:p-7">
        <div className="mb-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">History</p>
          <h3 className="mt-3 text-2xl font-semibold text-slate-950">Your attendance history</h3>
          <p className="mt-3 text-sm leading-7 text-slate-500">
            Review the attendance records you have created by filtering on class, student, and date range.
          </p>
        </div>

        <div className="grid gap-4 lg:grid-cols-4">
          <FormField label="Class">
            <Select
              value={historyFilters.classId}
              onChange={(event) => setHistoryFilters((current) => ({ ...current, classId: event.target.value }))}
              placeholder="All classes"
            >
              {availableClasses.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} {item.section}
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Student">
            <Select
              value={historyFilters.studentId}
              onChange={(event) => setHistoryFilters((current) => ({ ...current, studentId: event.target.value }))}
              placeholder="All students"
            >
              {students.map((student) => (
                <option key={student.id} value={student.id}>
                  {student.fullName}
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Start date">
            <Input
              type="date"
              value={historyFilters.startDate}
              onChange={(event) => setHistoryFilters((current) => ({ ...current, startDate: event.target.value }))}
            />
          </FormField>

          <FormField label="End date">
            <Input
              type="date"
              value={historyFilters.endDate}
              onChange={(event) => setHistoryFilters((current) => ({ ...current, endDate: event.target.value }))}
            />
          </FormField>
        </div>

        <div className="mt-6">
          {filteredHistory.length === 0 ? (
            <EmptyState
              title="No attendance history found"
              description="Try adjusting the filters or save attendance for one of your assigned classes."
            />
          ) : (
            <DataTable columns={historyColumns} rows={filteredHistory} getRowKey={(record) => record.id} />
          )}
        </div>
      </Card>
    </div>
  );
}
