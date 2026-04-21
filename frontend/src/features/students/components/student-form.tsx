"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { studentSchema, type StudentFormValues } from "@/features/students/schemas/student.schema";
import type { CreateStudentDto, Student, UpdateStudentDto } from "@/features/students/types/student.types";

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

function mapGenderToBackendValue(value: StudentFormValues["gender"]): 1 | 2 | 3 {
  switch (value) {
    case "Male":
      return 1;
    case "Female":
      return 2;
    case "Other":
      return 3;
  }
}

function mapGenderFromBackendValue(value?: Student["gender"]): StudentFormValues["gender"] {
  switch (value) {
    case 2:
      return "Female";
    case 3:
      return "Other";
    case 1:
    default:
      return "Male";
  }
}

interface StudentFormProps {
  mode: "create" | "edit";
  initialValues?: Student | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateStudentDto | UpdateStudentDto) => void;
}

export function StudentForm({ mode, initialValues, isSubmitting, onSubmit }: StudentFormProps) {
  const { data: classesData } = useClasses();
  const form = useForm<StudentFormValues>({
    resolver: zodResolver(studentSchema),
    defaultValues: {
      fullName: initialValues?.fullName ?? "",
      email: initialValues?.email ?? "",
      password: "",
      phone: initialValues?.phone ?? "",
      address: "",
      studentCode: initialValues?.studentCode ?? "",
      dateOfBirth: initialValues?.dateOfBirth ?? "",
      gender: mapGenderFromBackendValue(initialValues?.gender),
      admissionDate: initialValues?.admissionDate ?? "",
      parentId: initialValues?.parentId ?? "",
      classId: initialValues?.classId ?? ""
    }
  });

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = form;

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        const payload = {
          fullName: values.fullName,
          email: values.email,
          phone: toNullable(values.phone),
          address: toNullable(values.address),
          studentCode: values.studentCode,
          dateOfBirth: values.dateOfBirth,
          gender: mapGenderToBackendValue(values.gender),
          admissionDate: values.admissionDate,
          parentId: toNullable(values.parentId),
          classId: toNullable(values.classId)
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
      <FormField label="Student code" error={errors.studentCode?.message}>
        <Input {...register("studentCode")} />
      </FormField>
      <FormField label="Gender" error={errors.gender?.message}>
        <Select {...register("gender")}>
          <option value="Male">Male</option>
          <option value="Female">Female</option>
          <option value="Other">Other</option>
        </Select>
      </FormField>
      <FormField label="Date of birth" error={errors.dateOfBirth?.message}>
        <Input type="date" {...register("dateOfBirth")} />
      </FormField>
      <FormField label="Admission date" error={errors.admissionDate?.message}>
        <Input type="date" {...register("admissionDate")} />
      </FormField>
      <FormField label="Class" error={errors.classId?.message}>
        <Select {...register("classId")} placeholder="Select class">
          {classesData?.items.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} {item.section}
            </option>
          ))}
        </Select>
      </FormField>
      <FormField label="Parent ID" error={errors.parentId?.message}>
        <Input placeholder="Optional parent record ID" {...register("parentId")} />
      </FormField>
      <div className="md:col-span-2 flex justify-end gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create student" : "Update student"}
        </Button>
      </div>
    </form>
  );
}
