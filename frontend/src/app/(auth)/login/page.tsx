import { redirect } from "next/navigation";
import { LoginForm } from "@/features/auth/login-form";
import { getServerAuthSession } from "@/server/auth-session";
import { getRoleDashboardPath } from "@/utils/auth";

export default async function LoginPage() {
  const user = await getServerAuthSession();
  if (user) {
    redirect(getRoleDashboardPath(user.role));
  }

  return <LoginForm />;
}
