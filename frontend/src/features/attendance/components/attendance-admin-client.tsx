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
import { useStudents } from "@/features/students/hooks/use-students";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";
import { useTeachers } from "@/features/teachers/hooks/use-teachers";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";

const attendanceFilterSchema = z.object({
  classId: z.string().min(1, "Class is required"),
  subjectId: z.string().min(1, "Subject is required"),
  teacherId: z.string().min(1, "Teacher is required"),
  date: z.string().min(1, "Date is required"),
  rows: z.array(
    z.object({
      attendanceId: z.string().optional(),
      studentId: z.string(),
      studentName: z.string(),
      status: z.enum(["Present", "Absent", "Late"]),
      remarks: z.string().max(250, "Maximum 250 characters").optional().or(z.literal(""))
    })
  )
});

type AttendanceFormValues = z.infer<typeof attendanceFilterSchema>;

const statusOptions: AttendanceStatus[] = ["Present", "Absent", "Late"];

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function normaliseNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function AttendanceAdminClient() {
  const toast = useToast();
  const attendanceQuery = useAttendance();
  const classesQuery = useClasses();
  const studentsQuery = useStudents();
  const subjectsQuery = useSubjects();
  const teachersQuery = useTeachers();
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
    resolver: zodResolver(attendanceFilterSchema),
    defaultValues: {
      classId: "",
      subjectId: "",
      teacherId: "",
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
  const selectedTeacherId = watch("teacherId");
  const selectedDate = watch("date");

  const classes = useMemo(() => classesQuery.data?.items ?? [], [classesQuery.data?.items]);
  const students = useMemo(() => studentsQuery.data?.items ?? [], [studentsQuery.data?.items]);
  const subjects = useMemo(() => subjectsQuery.data?.items ?? [], [subjectsQuery.data?.items]);
  const teachers = useMemo(() => teachersQuery.data?.items ?? [], [teachersQuery.data?.items]);
  const attendanceRecords = useMemo(() => attendanceQuery.data?.items ?? [], [attendanceQuery.data?.items]);

  const selectedClass = classes.find((item) => item.id === selectedClassId);
  const classStudents = useMemo(
    () => students.filter((student) => student.classId === selectedClassId),
    [selectedClassId, students]
  );

  const derivedRows = useMemo(() => {
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
        status: (existingRecord?.status ?? "Present") as "Present" | "Absent" | "Late",
        remarks: existingRecord?.remarks ?? ""
      };
    });
  }, [attendanceRecords, classStudents, selectedClassId, selectedDate, selectedSubjectId]);

  useEffect(() => {
    replace(derivedRows);
  }, [derivedRows, replace]);

  useEffect(() => {
    if (selectedClass?.classTeacherId && !selectedTeacherId) {
      setValue("teacherId", selectedClass.classTeacherId);
    }
  }, [selectedClass?.classTeacherId, selectedTeacherId, setValue]);

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
    { key: "teacher", header: "Teacher", render: (record) => record.teacherName },
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
                : "bg-rose-50 text-rose-700"
          }
        >
          {record.status}
        </Badge>
      )
    }
  ];

  const isLoading =
    attendanceQuery.isLoading || classesQuery.isLoading || studentsQuery.isLoading || subjectsQuery.isLoading || teachersQuery.isLoading;

  if (isLoading) {
    return <LoadingState title="Loading attendance..." description="Preparing attendance registers and history records." />;
  }

  if (attendanceQuery.isError || classesQuery.isError || studentsQuery.isError || subjectsQuery.isError || teachersQuery.isError) {
    return (
      <EmptyState
        title="Unable to load attendance workspace"
        description="One or more attendance dependencies failed to load. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void attendanceQuery.refetch();
              void classesQuery.refetch();
              void studentsQuery.refetch();
              void subjectsQuery.refetch();
              void teachersQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Attendance"
        title="Attendance Register"
        description="Record daily attendance by class, subject, and date. Review and filter the full attendance history below."
      />

      <Card className="p-6 lg:p-7">
        <div className="mb-6">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Register</p>
          <h3 className="mt-3 text-2xl font-semibold text-slate-950">Mark Attendance</h3>
          <p className="mt-3 text-sm leading-7 text-slate-500">
            Record attendance for a class, subject, and date. Select the class and subject to load the student roster.
          </p>
        </div>
        
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
                    teacherId: values.teacherId,
                    date: values.date,
                    status: row.status,
                    remarks: normaliseNullable(row.remarks)
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
                toast.success("Attendance saved", "Attendance records were saved for the selected class.");
              } catch (error) {
                toast.error("Unable to save attendance", getApiErrorMessage(error));
              } finally {
                setIsSaving(false);
              }
            })();
          })}
        >
          <div className="grid gap-4 lg:grid-cols-4">
            <FormField label="Class" error={formState.errors.classId?.message}>
              <Select {...register("classId")} placeholder="Select class">
                {classes.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} {item.section}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Subject" error={formState.errors.subjectId?.message}>
              <Select {...register("subjectId")} placeholder="Select subject">
                {subjects.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Teacher" error={formState.errors.teacherId?.message}>
              <Select {...register("teacherId")} placeholder="Select teacher">
                {teachers.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.fullName}
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
              title="No students in the selected class"
              description="Choose a class with enrolled students to start marking attendance."
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
                      <p className="mt-1 text-sm text-slate-500">{selectedClass?.name} {selectedClass?.section}</p>
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
          <h3 className="mt-3 text-2xl font-semibold text-slate-950">Attendance history</h3>
          <p className="mt-3 text-sm leading-7 text-slate-500">
            Filter previous records by class, student, and date range to review attendance trends and spot exceptions.
          </p>
        </div>

        <div className="grid gap-4 lg:grid-cols-4">
          <FormField label="Class">
            <Select
              value={historyFilters.classId}
              onChange={(event) => setHistoryFilters((current) => ({ ...current, classId: event.target.value }))}
              placeholder="All classes"
            >
              {classes.map((item) => (
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
              description="Try adjusting the filters or save attendance for a class to start building history."
            />
          ) : (
            <DataTable columns={historyColumns} rows={filteredHistory} getRowKey={(record) => record.id} />
          )}
        </div>
      </Card>
    </div>
  );
}
