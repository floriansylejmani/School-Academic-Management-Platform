"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";
import { useTeachers } from "@/features/teachers/hooks/use-teachers";
import { timetableSchema, type TimetableFormValues } from "@/features/timetable/schemas/timetable.schema";
import type { TimetableEntry, CreateTimetableEntryDto, UpdateTimetableEntryDto } from "@/features/timetable/types/timetable.types";

const DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"] as const;

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

/**
 * <input type="time"> returns "HH:mm" but the backend's TimeOnly JSON
 * deserialiser requires "HH:mm:ss". Append ":00" when seconds are absent.
 */
function normalizeTime(time: string): string {
  if (!time) return time;
  return time.split(":").length === 2 ? `${time}:00` : time;
}

interface TimetableFormProps {
  mode: "create" | "edit";
  initialValues?: TimetableEntry | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateTimetableEntryDto | UpdateTimetableEntryDto) => void;
}

export function TimetableForm({ mode, initialValues, isSubmitting, onSubmit }: TimetableFormProps) {
  const { data: classesData } = useClasses();
  const { data: subjectsData } = useSubjects();
  const { data: teachersData } = useTeachers();

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<TimetableFormValues>({
    resolver: zodResolver(timetableSchema),
    defaultValues: {
      classId: initialValues?.classId ?? "",
      subjectId: initialValues?.subjectId ?? "",
      teacherId: initialValues?.teacherId ?? "",
      dayOfWeek: initialValues?.dayOfWeek ?? "Monday",
      startTime: initialValues?.startTime ?? "",
      endTime: initialValues?.endTime ?? "",
      roomNumber: initialValues?.roomNumber ?? ""
    }
  });

  const classes = classesData?.items ?? [];
  const subjects = subjectsData?.items ?? [];
  const teachers = teachersData?.items ?? [];

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          classId: values.classId,
          subjectId: values.subjectId,
          teacherId: values.teacherId,
          dayOfWeek: values.dayOfWeek,
          startTime: normalizeTime(values.startTime),
          endTime: normalizeTime(values.endTime),
          roomNumber: toNullable(values.roomNumber)
        });
      })}
    >
      <FormField label="Class" error={errors.classId?.message}>
        <Select {...register("classId")} placeholder="Select a class">
          {classes.map((cls) => (
            <option key={cls.id} value={cls.id}>
              {cls.name} {cls.section}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Subject" error={errors.subjectId?.message}>
        <Select {...register("subjectId")} placeholder="Select a subject">
          {subjects.map((subject) => (
            <option key={subject.id} value={subject.id}>
              {subject.name}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Teacher" error={errors.teacherId?.message}>
        <Select {...register("teacherId")} placeholder="Select a teacher">
          {teachers.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.fullName}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Day of week" error={errors.dayOfWeek?.message}>
        <Select {...register("dayOfWeek")}>
          {DAYS_OF_WEEK.map((day) => (
            <option key={day} value={day}>
              {day}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Start time" error={errors.startTime?.message}>
        <Input {...register("startTime")} type="time" />
      </FormField>

      <FormField label="End time" error={errors.endTime?.message}>
        <Input {...register("endTime")} type="time" />
      </FormField>

      <FormField label="Room number" error={errors.roomNumber?.message}>
        <Input {...register("roomNumber")} placeholder="e.g. A-101" />
      </FormField>

      <div className="flex items-end md:col-span-2">
        <Button type="submit" disabled={isSubmitting} className="ml-auto">
          {isSubmitting ? (mode === "create" ? "Creating..." : "Saving...") : mode === "create" ? "Create entry" : "Save changes"}
        </Button>
      </div>
    </form>
  );
}
