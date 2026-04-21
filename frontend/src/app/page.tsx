import { redirect } from "next/navigation";
import { getServerAuthSession } from "@/server/auth-session";
import { getRoleDashboardPath } from "@/utils/auth";

export default async function HomePage() {
  const user = await getServerAuthSession();
  if (user) {
    redirect(getRoleDashboardPath(user.role));
  }

  redirect("/login");
}
