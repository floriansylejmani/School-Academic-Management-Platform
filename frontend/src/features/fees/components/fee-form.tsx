"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { useStudents } from "@/features/students/hooks/use-students";
import { feeSchema, type FeeFormValues } from "@/features/fees/schemas/fee.schema";
import type { CreateFeeDto, Fee, FeeStatus, FeeStatusValue, UpdateFeeDto } from "@/features/fees/types/fees.types";

const FEE_STATUSES: FeeStatus[] = ["Pending", "Paid", "Overdue", "PartiallyPaid"];

function mapFeeStatusToBackendValue(status: FeeStatus): FeeStatusValue {
  switch (status) {
    case "PartiallyPaid":
      return 2;
    case "Paid":
      return 3;
    case "Overdue":
      return 4;
    case "Pending":
    default:
      return 1;
  }
}

function getFeeStatusLabel(status: FeeStatus) {
  return status === "PartiallyPaid" ? "Partial" : status;
}

interface FeeFormProps {
  mode: "create" | "edit";
  initialValues?: Fee | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateFeeDto | UpdateFeeDto) => void;
}

export function FeeForm({ mode, initialValues, isSubmitting, onSubmit }: FeeFormProps) {
  const studentsQuery = useStudents();
  const students = studentsQuery.data?.items ?? [];

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<FeeFormValues>({
    resolver: zodResolver(feeSchema),
    defaultValues: {
      studentId: initialValues?.studentId ?? "",
      feeType: initialValues?.feeType ?? "",
      amount: initialValues?.amount ?? 0,
      dueDate: initialValues?.dueDate ?? "",
      status: initialValues?.status ?? "Pending"
    }
  });

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          studentId: values.studentId,
          feeType: values.feeType,
          amount: values.amount,
          dueDate: values.dueDate,
          status: mapFeeStatusToBackendValue(values.status)
        });
      })}
    >
      <FormField label="Student" error={errors.studentId?.message}>
        <Select {...register("studentId")} placeholder="Select student">
          {students.map((student) => (
            <option key={student.id} value={student.id}>
              {student.fullName}
            </option>
          ))}
        </Select>
      </FormField>
      <FormField label="Fee type" error={errors.feeType?.message}>
        <Input placeholder="Tuition, exam fee, transport..." {...register("feeType")} />
      </FormField>
      <FormField label="Amount" error={errors.amount?.message}>
        <Input type="number" step="0.01" min="0" {...register("amount")} />
      </FormField>
      <FormField label="Due date" error={errors.dueDate?.message}>
        <Input type="date" {...register("dueDate")} />
      </FormField>
      <FormField label="Status" error={errors.status?.message}>
        <Select {...register("status")}>
          {FEE_STATUSES.map((status) => (
            <option key={status} value={status}>
              {getFeeStatusLabel(status)}
            </option>
          ))}
        </Select>
      </FormField>
      <div className="md:col-span-2 flex justify-end">
        <Button type="submit" disabled={isSubmitting || studentsQuery.isLoading}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create fee" : "Update fee"}
        </Button>
      </div>
    </form>
  );
}
