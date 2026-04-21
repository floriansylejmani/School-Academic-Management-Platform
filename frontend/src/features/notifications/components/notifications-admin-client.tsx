"use client";

import { useMemo, useState } from "react";
import { Send } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useSendNotification } from "@/features/notifications/hooks/use-notifications";
import { useParents } from "@/features/parents/hooks/use-parents";
import { useStudents } from "@/features/students/hooks/use-students";
import { useTeachers } from "@/features/teachers/hooks/use-teachers";

type TargetMode = "user" | "role";
type RoleName = "Admin" | "Teacher" | "Student" | "Parent";

const ROLES: RoleName[] = ["Admin", "Teacher", "Student", "Parent"];

const sendSchema = z
  .object({
    title: z.string().min(1, "Title is required").max(100, "Title must be at most 100 characters"),
    message: z.string().min(1, "Message is required").max(500, "Message must be at most 500 characters"),
    targetMode: z.enum(["user", "role"]),
    userId: z.string().optional(),
    roleName: z.enum(["Admin", "Teacher", "Student", "Parent"]).optional()
  })
  .superRefine((data, ctx) => {
    if (data.targetMode === "user" && !data.userId?.trim()) {
      ctx.addIssue({ code: "custom", path: ["userId"], message: "Recipient is required" });
    }
  });

type SendFormValues = z.infer<typeof sendSchema>;

export function NotificationsAdminClient() {
  const [targetMode, setTargetMode] = useState<TargetMode>("role");
  const sendNotification = useSendNotification();
  const teachersQuery = useTeachers();
  const studentsQuery = useStudents();
  const parentsQuery = useParents();

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors }
  } = useForm<SendFormValues>({
    resolver: zodResolver(sendSchema),
    defaultValues: { targetMode: "role", roleName: "Student", userId: "" }
  });

  const recipientOptions = useMemo(() => {
    const teachers = (teachersQuery.data?.items ?? []).map((teacher) => ({
      value: teacher.userId,
      label: `${teacher.fullName} (${teacher.email})`,
      role: "Teacher" as const
    }));
    const students = (studentsQuery.data?.items ?? []).map((student) => ({
      value: student.userId,
      label: `${student.fullName} (${student.email})`,
      role: "Student" as const
    }));
    const parents = (parentsQuery.data?.items ?? []).map((parent) => ({
      value: parent.userId,
      label: `${parent.fullName} (${parent.email})`,
      role: "Parent" as const
    }));

    return [...teachers, ...students, ...parents].sort((left, right) =>
      left.label.localeCompare(right.label)
    );
  }, [parentsQuery.data?.items, studentsQuery.data?.items, teachersQuery.data?.items]);

  const recipientsLoading =
    teachersQuery.isLoading || studentsQuery.isLoading || parentsQuery.isLoading;
  const recipientsError = teachersQuery.isError || studentsQuery.isError || parentsQuery.isError;

  const onSubmit = async (values: SendFormValues) => {
    try {
      await sendNotification.mutateAsync({
        title: values.title,
        message: values.message,
        userId: values.targetMode === "user" ? values.userId : undefined,
        roleName: values.targetMode === "role" ? values.roleName : undefined
      });
      reset({ targetMode, roleName: "Student", userId: "" });
    } catch {
      // handled by hook toast
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Communications"
        title="Broadcast"
        description="Send a targeted message to an individual user, or broadcast an announcement to an entire role group."
      />

      <Card className="max-w-2xl p-6">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div>
            <p className="mb-2 text-sm font-medium text-slate-700">Recipient</p>
            <div className="flex gap-2">
              {(["role", "user"] as TargetMode[]).map((mode) => (
                <Button
                  key={mode}
                  type="button"
                  variant={targetMode === mode ? "primary" : "secondary"}
                  onClick={() => {
                    setTargetMode(mode);
                    setValue("targetMode", mode, { shouldValidate: true });

                    if (mode === "role") {
                      setValue("roleName", "Student", { shouldValidate: true });
                      setValue("userId", "", { shouldValidate: false });
                    }
                  }}
                >
                  {mode === "role" ? "Broadcast to role" : "Single user"}
                </Button>
              ))}
            </div>
          </div>

          {targetMode === "role" ? (
            <FormField label="Role" error={errors.roleName?.message}>
              <div className="flex flex-wrap gap-2">
                {ROLES.map((role) => (
                  <label key={role} className="flex cursor-pointer items-center gap-2">
                    <input
                      type="radio"
                      value={role}
                      className="accent-brand-600"
                      {...register("roleName")}
                    />
                    <span className="text-sm text-slate-700">{role}</span>
                  </label>
                ))}
              </div>
            </FormField>
          ) : (
            <FormField label="Recipient" error={errors.userId?.message}>
              {recipientsLoading ? (
                <p className="text-sm text-slate-500">Loading available recipients...</p>
              ) : recipientsError ? (
                <div className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
                  The user list could not be loaded. Try again before sending a direct notification.
                </div>
              ) : recipientOptions.length === 0 ? (
                <div className="rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-600">
                  There are no teacher, student, or parent accounts available for direct delivery.
                </div>
              ) : (
                <Select {...register("userId")} placeholder="Select a user">
                  {recipientOptions.map((recipient) => (
                    <option key={recipient.value} value={recipient.value}>
                      {recipient.role} - {recipient.label}
                    </option>
                  ))}
                </Select>
              )}
            </FormField>
          )}

          <input type="hidden" value={targetMode} {...register("targetMode")} />

          <FormField label="Title" error={errors.title?.message}>
            <Input placeholder="e.g. School closure tomorrow" {...register("title")} />
          </FormField>

          <FormField label="Message" error={errors.message?.message}>
            <textarea
              className="resize-none w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
              rows={4}
              placeholder="Full notification text (max 500 characters)."
              maxLength={500}
              {...register("message")}
            />
          </FormField>

          <Button
            type="submit"
            disabled={
              sendNotification.isPending ||
              (targetMode === "user" &&
                (recipientsLoading || recipientsError || recipientOptions.length === 0))
            }
            className="gap-2"
          >
            <Send className="h-4 w-4" />
            {sendNotification.isPending ? "Sending..." : "Send notification"}
          </Button>
        </form>
      </Card>
    </div>
  );
}
