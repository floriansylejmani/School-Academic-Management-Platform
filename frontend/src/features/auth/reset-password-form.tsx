"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useToast } from "@/hooks/use-toast";
import { authService } from "@/services/auth.service";
import { resetPasswordSchema, type ResetPasswordSchema } from "@/features/auth/reset-password.schema";
import { getApiErrorMessage } from "@/utils/api";

export function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const toast = useToast();
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors }
  } = useForm<ResetPasswordSchema>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      token: "",
      newPassword: "",
      confirmPassword: ""
    }
  });

  useEffect(() => {
    const token = searchParams.get("token");
    if (token) {
      setValue("token", token);
    }
  }, [searchParams, setValue]);

  const resetPasswordMutation = useMutation({
    mutationFn: authService.resetPassword,
    onSuccess: () => {
      toast.success("Password reset complete", "Sign in with your new password.");
      router.replace("/login");
    },
    onError: (error) => {
      toast.error("Unable to reset password", getApiErrorMessage(error));
    }
  });

  return (
    <Card className="w-full max-w-[520px] p-8 sm:p-10">
      <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Password Reset</p>
      <h1 className="mt-3 text-3xl font-semibold text-slate-950">Choose a new password</h1>
      <p className="mt-4 text-sm leading-7 text-slate-500">
        Paste the token from your reset link, or open this page directly from the link in your email.
      </p>

      <form className="mt-8 space-y-5" onSubmit={handleSubmit((values) => resetPasswordMutation.mutate(values))}>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700" htmlFor="token">
            Reset token
          </label>
          <Input
            id="token"
            placeholder="Paste your reset token"
            aria-invalid={Boolean(errors.token)}
            {...register("token")}
          />
          {errors.token ? <p className="text-sm text-rose-600">{errors.token.message}</p> : null}
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700" htmlFor="newPassword">
            New password
          </label>
          <Input
            id="newPassword"
            type="password"
            placeholder="Enter a new password"
            aria-invalid={Boolean(errors.newPassword)}
            {...register("newPassword")}
          />
          {errors.newPassword ? <p className="text-sm text-rose-600">{errors.newPassword.message}</p> : null}
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700" htmlFor="confirmPassword">
            Confirm password
          </label>
          <Input
            id="confirmPassword"
            type="password"
            placeholder="Repeat your new password"
            aria-invalid={Boolean(errors.confirmPassword)}
            {...register("confirmPassword")}
          />
          {errors.confirmPassword ? (
            <p className="text-sm text-rose-600">{errors.confirmPassword.message}</p>
          ) : null}
        </div>

        <Button className="w-full" type="submit" disabled={resetPasswordMutation.isPending}>
          {resetPasswordMutation.isPending ? "Resetting password..." : "Reset password"}
        </Button>
      </form>

      <div className="mt-8 flex flex-col gap-3">
        <Link href="/forgot-password" className="block">
          <Button className="w-full" variant="secondary">
            Request another link
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
