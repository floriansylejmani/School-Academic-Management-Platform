"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { parentSchema, type ParentFormValues } from "@/features/parents/schemas/parents.schema";
import type { CreateParentDto, Parent, UpdateParentDto } from "@/features/parents/types/parents.types";

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

export function ParentForm({
  mode,
  initialValues,
  isSubmitting,
  onSubmit
}: {
  mode: "create" | "edit";
  initialValues?: Parent | null;
  isSubmitting: boolean;
  onSubmit: (payload: CreateParentDto | UpdateParentDto) => void;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<ParentFormValues>({
    resolver: zodResolver(parentSchema),
    defaultValues: {
      fullName: initialValues?.fullName ?? "",
      email: initialValues?.email ?? "",
      password: mode === "create" ? "" : undefined,
      phone: initialValues?.phone ?? "",
      address: initialValues?.address ?? "",
      occupation: initialValues?.occupation ?? ""
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
          occupation: toNullable(values.occupation)
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
      <FormField label="Occupation" error={errors.occupation?.message}>
        <Input {...register("occupation")} />
      </FormField>
      <div className="md:col-span-2 flex justify-end">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : mode === "create" ? "Create parent" : "Update parent"}
        </Button>
      </div>
    </form>
  );
}
