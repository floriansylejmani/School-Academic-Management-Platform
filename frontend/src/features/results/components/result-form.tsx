"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { useExams } from "@/features/exams/hooks/use-exams";
import type { Exam } from "@/features/exams/types/exams.types";
import { useStudents } from "@/features/students/hooks/use-students";
import type { Student } from "@/features/students/types/student.types";
import { resultSchema, type ResultFormValues } from "@/features/results/schemas/results.schema";
import type { Result, CreateResultDto, UpdateResultDto, Grade } from "@/features/results/types/results.types";

const GRADES: Grade[] = ["A+", "A", "B+", "B", "C+", "C", "D", "F"];

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

interface ResultFormProps {
  mode: "create" | "edit";
  initialValues?: Result | null;
  isSubmitting: boolean;
  exams?: Exam[];
  students?: Student[];
  onSubmit: (payload: CreateResultDto | UpdateResultDto) => void;
}

export function ResultForm({ mode, initialValues, isSubmitting, exams: providedExams, students: providedStudents, onSubmit }: ResultFormProps) {
  const { data: examsData } = useExams();
  const { data: studentsData } = useStudents();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors }
  } = useForm<ResultFormValues>({
    resolver: zodResolver(resultSchema),
    defaultValues: {
      examId: initialValues?.examId ?? "",
      studentId: initialValues?.studentId ?? "",
      marksObtained: initialValues?.marksObtained ?? 0,
      grade: initialValues?.grade ?? "A",
      remarks: initialValues?.remarks ?? ""
    }
  });

  const exams = providedExams ?? examsData?.items ?? [];
  const students = providedStudents ?? studentsData?.items ?? [];
  const selectedExamId = watch("examId");
  const selectedStudentId = watch("studentId");
  const selectedExam = exams.find((exam) => exam.id === selectedExamId);

  const filteredStudents = useMemo(() => {
    if (!selectedExam) {
      return students;
    }

    return students.filter((student) => student.classId === selectedExam.classId);
  }, [selectedExam, students]);

  useEffect(() => {
    const hasCurrentStudent = filteredStudents.some((student) => student.id === selectedStudentId);

    if (!hasCurrentStudent) {
      setValue("studentId", "");
    }
  }, [filteredStudents, selectedStudentId, setValue]);

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          examId: values.examId,
          studentId: values.studentId,
          marksObtained: values.marksObtained,
          grade: values.grade,
          remarks: toNullable(values.remarks)
        });
      })}
    >
      <FormField label="Exam" error={errors.examId?.message}>
        <Select {...register("examId")} placeholder="Select an exam">
          {exams.map((exam) => (
            <option key={exam.id} value={exam.id}>
              {exam.title} — {exam.className}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Student" error={errors.studentId?.message}>
        <Select {...register("studentId")} placeholder="Select a student">
          {filteredStudents.map((student) => (
            <option key={student.id} value={student.id}>
              {student.fullName} ({student.studentCode})
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Marks obtained" error={errors.marksObtained?.message}>
        <Input
          {...register("marksObtained", { valueAsNumber: true })}
          type="number"
          min={0}
          max={1000}
          placeholder="0"
        />
      </FormField>

      <FormField label="Grade" error={errors.grade?.message}>
        <Select {...register("grade")}>
          {GRADES.map((grade) => (
            <option key={grade} value={grade}>
              {grade}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField label="Remarks" error={errors.remarks?.message}>
        <Input {...register("remarks")} placeholder="Optional comments" />
      </FormField>

      <div className="flex items-end md:col-span-2">
        <Button type="submit" disabled={isSubmitting} className="ml-auto">
          {isSubmitting
            ? mode === "create"
              ? "Recording..."
              : "Saving..."
            : mode === "create"
              ? "Record result"
              : "Save changes"}
        </Button>
      </div>
    </form>
  );
}
