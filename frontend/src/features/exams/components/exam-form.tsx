"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { useClasses } from "@/features/classes/hooks/use-classes";
import type { AcademicClass } from "@/features/classes/types/class.types";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";
import type { Subject } from "@/features/subjects/types/subject.types";
import { examSchema, type ExamFormValues } from "@/features/exams/schemas/exams.schema";
import type { Exam, CreateExamDto, UpdateExamDto } from "@/features/exams/types/exams.types";

interface ExamFormProps {
  mode: "create" | "edit";
  initialValues?: Exam | null;
  isSubmitting: boolean;
  classes?: AcademicClass[];
  subjects?: Subject[];
  onSubmit: (payload: CreateExamDto | UpdateExamDto) => void;
}

export function ExamForm({ mode, initialValues, isSubmitting, classes: providedClasses, subjects: providedSubjects, onSubmit }: ExamFormProps) {
  const { data: classesData } = useClasses();
  const { data: subjectsData } = useSubjects();

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<ExamFormValues>({
    resolver: zodResolver(examSchema),
    defaultValues: {
      title: initialValues?.title ?? "",
      classId: initialValues?.classId ?? "",
      subjectId: initialValues?.subjectId ?? "",
      examDate: initialValues?.examDate ?? "",
      totalMarks: initialValues?.totalMarks ?? 100
    }
  });

  const classes = providedClasses ?? classesData?.items ?? [];
  const subjects = providedSubjects ?? subjectsData?.items ?? [];

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          title: values.title,
          classId: values.classId,
          subjectId: values.subjectId,
          examDate: values.examDate,
          totalMarks: values.totalMarks
        });
      })}
    >
      <FormField label="Exam title" error={errors.title?.message}>
        <Input {...register("title")} placeholder="e.g. Mid-Term Mathematics" />
      </FormField>

      <FormField label="Total marks" error={errors.totalMarks?.message}>
        <Input
          {...register("totalMarks", { valueAsNumber: true })}
          type="number"
          min={1}
          max={1000}
          placeholder="100"
        />
      </FormField>

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

      <FormField label="Exam date" error={errors.examDate?.message}>
        <Input {...register("examDate")} type="date" />
      </FormField>

      <div className="flex items-end md:col-span-2">
        <Button type="submit" disabled={isSubmitting} className="ml-auto">
          {isSubmitting
            ? mode === "create"
              ? "Creating..."
              : "Saving..."
            : mode === "create"
              ? "Create exam"
              : "Save changes"}
        </Button>
      </div>
    </form>
  );
}
