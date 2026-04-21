"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { CircleCheck } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useToast } from "@/hooks/use-toast";
import { authService } from "@/services/auth.service";
import { forgotPasswordSchema, type ForgotPasswordSchema } from "@/features/auth/forgot-password.schema";
import { getApiErrorMessage } from "@/utils/api";

export function ForgotPasswordForm() {
  const toast = useToast();
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<ForgotPasswordSchema>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: ""
    }
  });

  const forgotPasswordMutation = useMutation({
    mutationFn: authService.forgotPassword,
    onSuccess: (response, variables) => {
      setSubmittedEmail(variables.email);
      toast.success("Reset requested", response.message);
    },
    onError: (error) => {
      toast.error("Unable to request password reset", getApiErrorMessage(error));
    }
  });

  return (
    <Card className="w-full max-w-[520px] p-8 sm:p-10">
      <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Account Recovery</p>
      <h1 className="mt-3 text-3xl font-semibold text-slate-950">Reset your password</h1>
      <p className="mt-4 text-sm leading-7 text-slate-500">
        Enter your school account email. A reset link will be issued if the address matches an active account. Check
        your email or the backend logs in local development.
      </p>

      <form className="mt-8 space-y-5" onSubmit={handleSubmit((values) => forgotPasswordMutation.mutate(values))}>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700" htmlFor="email">
            Email
          </label>
          <Input
            id="email"
            type="email"
            placeholder="you@school.com"
            aria-invalid={Boolean(errors.email)}
            {...register("email")}
          />
          {errors.email ? <p className="text-sm text-rose-600">{errors.email.message}</p> : null}
        </div>

        {submittedEmail ? (
          <div className="flex items-start gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
            <CircleCheck className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              Reset instructions were sent for{" "}
              <span className="font-semibold">{submittedEmail}</span>. Use the link from your email or the backend
              log, then complete the form on the reset page.
            </span>
          </div>
        ) : null}

        <Button className="w-full" type="submit" disabled={forgotPasswordMutation.isPending}>
          {forgotPasswordMutation.isPending ? "Requesting reset..." : "Send reset link"}
        </Button>
      </form>

      <div className="mt-8 flex flex-col gap-3">
        <Link href="/reset-password" className="block">
          <Button className="w-full" variant="secondary">
            I already have a token
          </Button>
        </Link>

        <Link href="/login" className="block">
          <Button className="w-full" variant="ghost">
            Back to login
          </Button>
        </Link>
      </div>
    </Card>
  );
}
