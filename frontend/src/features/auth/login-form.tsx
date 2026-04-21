"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { CircleAlert, Users, BookOpen, GraduationCap, Shield } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { authService } from "@/services/auth.service";
import { useAuthStore } from "@/store/auth.store";
import { getRoleDashboardPath } from "@/utils/auth";
import { LoginSchema, loginSchema } from "@/features/auth/login.schema";
import { getApiErrorMessage } from "@/utils/api";

export function LoginForm() {
  const router = useRouter();
  const initializeSession = useAuthStore((state) => state.initializeSession);
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<LoginSchema>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "admin@school.com",
      password: "Admin@12345"
    }
  });

  const loginMutation = useMutation({
    mutationFn: authService.login,
    onSuccess: (user) => {
      initializeSession(user);
      router.replace(getRoleDashboardPath(user.role));
      router.refresh();
    }
  });

  return (
    <Card className="w-full max-w-[520px] p-8 sm:p-10">
      <div className="mb-8">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Welcome Back</p>
        <h1 className="mt-3 text-3xl font-semibold text-slate-950">Sign in to your workspace</h1>
        <p className="mt-3 text-sm leading-6 text-slate-500">
          Access dashboards, attendance, results, and finance modules with the account issued by your school
          administrator.
        </p>
      </div>

      <form className="space-y-5" onSubmit={handleSubmit((values) => loginMutation.mutate(values))}>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700" htmlFor="email">
            Email
          </label>
          <Input
            id="email"
            type="email"
            placeholder="admin@school.com"
            aria-invalid={Boolean(errors.email)}
            {...register("email")}
          />
          {errors.email ? <p className="text-sm text-rose-600">{errors.email.message}</p> : null}
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium text-slate-700" htmlFor="password">
              Password
            </label>
            <Link href="/forgot-password" className="text-sm font-medium text-brand-700 hover:text-brand-800">
              Forgot password?
            </Link>
          </div>
          <Input
            id="password"
            type="password"
            placeholder="Enter your password"
            aria-invalid={Boolean(errors.password)}
            {...register("password")}
          />
          {errors.password ? <p className="text-sm text-rose-600">{errors.password.message}</p> : null}
        </div>

        {loginMutation.isError ? (
          <div className="flex items-start gap-3 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{getApiErrorMessage(loginMutation.error, "Unable to sign in. Check your credentials and try again.")}</span>
          </div>
        ) : null}

        <Button className="w-full" type="submit" disabled={loginMutation.isPending}>
          {loginMutation.isPending ? "Signing in..." : "Sign in"}
        </Button>
      </form>

      <div className="mt-8 space-y-4">
        <div className="rounded-[24px] bg-slate-50 p-5">
          <p className="text-sm font-semibold text-slate-900">Demo access</p>
          <p className="mt-2 text-sm leading-6 text-slate-500">
            A default admin account is created on first run. Use{" "}
            <span className="font-semibold text-slate-700">admin@school.com</span> and{" "}
            <span className="font-semibold text-slate-700">Admin@12345</span>. New accounts are created by an authenticated
            administrator — public sign-up is disabled.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <Card className="p-4 bg-gradient-to-br from-blue-50 to-blue-100 border-blue-200">
            <div className="flex items-center gap-3">
              <div className="shrink-0 rounded-lg bg-blue-100 p-2 text-blue-700">
                <Shield className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">Admin Dashboard</p>
                <p className="text-xs text-slate-600">School management overview</p>
              </div>
            </div>
          </Card>

          <Card className="p-4 bg-gradient-to-br from-emerald-50 to-emerald-100 border-emerald-200">
            <div className="flex items-center gap-3">
              <div className="shrink-0 rounded-lg bg-emerald-100 p-2 text-emerald-700">
                <BookOpen className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">Teacher Portal</p>
                <p className="text-xs text-slate-600">Attendance & grade management</p>
              </div>
            </div>
          </Card>

          <Card className="p-4 bg-gradient-to-br from-purple-50 to-purple-100 border-purple-200">
            <div className="flex items-center gap-3">
              <div className="shrink-0 rounded-lg bg-purple-100 p-2 text-purple-700">
                <GraduationCap className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">Student Portal</p>
                <p className="text-xs text-slate-600">Grades & schedule tracking</p>
              </div>
            </div>
          </Card>

          <Card className="p-4 bg-gradient-to-br from-orange-50 to-orange-100 border-orange-200">
            <div className="flex items-center gap-3">
              <div className="shrink-0 rounded-lg bg-orange-100 p-2 text-orange-700">
                <Users className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">Parent Portal</p>
                <p className="text-xs text-slate-600">Child progress monitoring</p>
              </div>
            </div>
          </Card>
        </div>

        <div className="rounded-[24px] bg-gradient-to-r from-brand-50 to-brand-100 p-5 border border-brand-200">
          <p className="text-sm font-semibold text-brand-900">🚀 Demo Ready</p>
          <p className="mt-2 text-sm leading-6 text-brand-700">
            This system includes comprehensive sample data with 24 students, 4 teachers, 8 parents, and 6 months of
            academic records. Perfect for showcasing all features in a live demo or client presentation.
          </p>
        </div>
      </div>
    </Card>
  );
}
