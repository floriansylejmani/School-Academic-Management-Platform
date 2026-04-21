"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { teacherSchema, type TeacherFormValues } from "@/features/teachers/schemas/teacher.schema";
import type { CreateTeacherDto, Teacher, UpdateTeacherDto } from "@/features/teachers/types/teacher.types";

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function TeacherForm({
  mode,
  initialValues,
  isSubmitting,
  onSubmit
}: {
  mode: "create" | "edit";
  initialValues?: Teacher | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateTeacherDto | UpdateTeacherDto) => void;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<TeacherFormValues>({
    resolver: zodResolver(teacherSchema),
    defaultValues: {
      fullName: initialValues?.fullName ?? "",
      email: initialValues?.email ?? "",
      password: "",
      phone: initialValues?.phone ?? "",
      address: "",
      teacherCode: initialValues?.teacherCode ?? "",
      specialization: initialValues?.specialization ?? "",
      hireDate: initialValues?.hireDate ?? ""
    }
  });

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        const payload = {
          fullName: values.fullName,
          email: values.email,
          phone: toNullable(values.phone),
          address: toNullable(values.address),
          teacherCode: values.teacherCode,
          specialization: values.specialization,
          hireDate: values.hireDate
        };

        if (mode === "create") {
          onSubmit({ ...payload, password: values.password ?? "" });
          return;
        }

        onSubmit(payload);
      })}
    >
      <FormField label="Full name" error={errors.fullName?.message}>
        <Input {...register("fullName")} />
      </FormField>
      <FormField label="Email" error={errors.email?.message}>
        <Input type="email" {...register("email")} />
      </FormField>
      {mode === "create" ? (
        <FormField label="Password" error={errors.password?.message}>
          <Input type="password" {...register("password")} />
        </FormField>
      ) : null}
      <FormField label="Phone" error={errors.phone?.message}>
        <Input {...register("phone")} />
      </FormField>
      <div className="md:col-span-2">
        <FormField label="Address" error={errors.address?.message}>
          <Input {...register("address")} />
        </FormField>
      </div>
      <FormField label="Teacher code" error={errors.teacherCode?.message}>
        <Input {...register("teacherCode")} />
      </FormField>
      <FormField label="Specialization" error={errors.specialization?.message}>
        <Input {...register("specialization")} />
      </FormField>
      <FormField label="Hire date" error={errors.hireDate?.message}>
        <Input type="date" {...register("hireDate")} />
      </FormField>
      <div className="md:col-span-2 flex justify-end">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create teacher" : "Update teacher"}
        </Button>
      </div>
    </form>
  );
}
