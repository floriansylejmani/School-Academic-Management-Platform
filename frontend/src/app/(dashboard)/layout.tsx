import { redirect } from "next/navigation";
import { DashboardShell } from "@/components/layout/dashboard-shell";
import { ProtectedRoute } from "@/components/layout/protected-route";
import { getServerAuthSession } from "@/server/auth-session";

export default async function AppDashboardLayout({ children }: { children: React.ReactNode }) {
  const user = await getServerAuthSession();
  if (!user) {
    redirect("/login");
  }

  return (
    <ProtectedRoute initialUser={user}>
      <DashboardShell>{children}</DashboardShell>
    </ProtectedRoute>
  );
}
