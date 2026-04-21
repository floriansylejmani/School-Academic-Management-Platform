"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { classSchema, type ClassFormValues } from "@/features/classes/schemas/class.schema";
import type { AcademicClass, CreateAcademicClassDto, UpdateAcademicClassDto } from "@/features/classes/types/class.types";
import { useTeachers } from "@/features/teachers/hooks/use-teachers";

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function ClassForm({
  mode,
  initialValues,
  isSubmitting,
  onSubmit
}: {
  mode: "create" | "edit";
  initialValues?: AcademicClass | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateAcademicClassDto | UpdateAcademicClassDto) => void;
}) {
  const { data: teachersData } = useTeachers();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<ClassFormValues>({
    resolver: zodResolver(classSchema),
    defaultValues: {
      name: initialValues?.name ?? "",
      section: initialValues?.section ?? "",
      academicYear: initialValues?.academicYear ?? "",
      classTeacherId: initialValues?.classTeacherId ?? ""
    }
  });

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          name: values.name,
          section: values.section,
          academicYear: values.academicYear,
          classTeacherId: toNullable(values.classTeacherId)
        });
      })}
    >
      <FormField label="Class name" error={errors.name?.message}>
        <Input placeholder="Grade 10" {...register("name")} />
      </FormField>
      <FormField label="Section" error={errors.section?.message}>
        <Input placeholder="A" {...register("section")} />
      </FormField>
      <FormField label="Academic year" error={errors.academicYear?.message}>
        <Input placeholder="2025/2026" {...register("academicYear")} />
      </FormField>
      <FormField label="Class teacher" error={errors.classTeacherId?.message}>
        <Select {...register("classTeacherId")} placeholder="Select teacher">
          {teachersData?.items.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.fullName}
            </option>
          ))}
        </Select>
      </FormField>
      <div className="md:col-span-2 flex justify-end">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create class" : "Update class"}
        </Button>
      </div>
    </form>
  );
}
