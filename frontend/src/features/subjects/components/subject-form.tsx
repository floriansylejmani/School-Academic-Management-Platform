"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { subjectSchema, type SubjectFormValues } from "@/features/subjects/schemas/subject.schema";
import type { CreateSubjectDto, Subject, UpdateSubjectDto } from "@/features/subjects/types/subject.types";

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function SubjectForm({
  mode,
  initialValues,
  isSubmitting,
  onSubmit
}: {
  mode: "create" | "edit";
  initialValues?: Subject | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateSubjectDto | UpdateSubjectDto) => void;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<SubjectFormValues>({
    resolver: zodResolver(subjectSchema),
    defaultValues: {
      name: initialValues?.name ?? "",
      code: initialValues?.code ?? "",
      description: initialValues?.description ?? ""
    }
  });

  return (
    <form
      className="grid gap-5"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          name: values.name,
          code: values.code,
          description: toNullable(values.description)
        });
      })}
    >
      <FormField label="Subject name" error={errors.name?.message}>
        <Input {...register("name")} />
      </FormField>
      <FormField label="Subject code" error={errors.code?.message}>
        <Input {...register("code")} />
      </FormField>
      <FormField label="Description" error={errors.description?.message}>
        <Input {...register("description")} />
      </FormField>
      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create subject" : "Update subject"}
        </Button>
      </div>
    </form>
  );
}
