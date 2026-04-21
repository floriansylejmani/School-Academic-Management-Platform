"use client";

import { LogOut, Menu } from "lucide-react";
import { useRouter } from "next/navigation";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { NotificationBell } from "@/components/ui/notification-bell";
import { useAuthStore } from "@/store/auth.store";
import { useUiStore } from "@/store/ui.store";
import type { UserRole } from "@/types/auth";

const notificationRoutes: Partial<Record<UserRole, string>> = {
  Admin: "/admin/notifications",
  Teacher: "/teacher/notifications",
  Student: "/student/notifications",
  Parent: "/parent/notifications"
};

export function DashboardHeader() {
  const router = useRouter();
  const { toggleSidebar } = useUiStore();
  const { logout, user } = useAuthStore();

  const handleLogout = () => {
    void (async () => {
      await logout();
      router.replace("/login");
      router.refresh();
    })();
  };

  return (
    <header className="sticky top-0 z-20 border-b border-white/60 bg-slate-50/85 backdrop-blur-xl">
      <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={toggleSidebar}
            className="inline-flex h-10 w-10 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-1 lg:hidden"
            aria-label="Toggle sidebar"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-600">Scholara</p>
            <h1 className="text-base font-semibold text-slate-900">Academic Management Platform</h1>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {user?.role && notificationRoutes[user.role] ? (
            <NotificationBell href={notificationRoutes[user.role]!} />
          ) : null}

          <div className="hidden rounded-2xl border border-slate-200 bg-white px-4 py-2 md:block">
            <p className="text-sm font-semibold text-slate-900">{user?.fullName}</p>
            <div className="mt-0.5">
              <Badge>{user?.role}</Badge>
            </div>
          </div>

          <Button variant="secondary" className="gap-2" onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            <span className="hidden sm:inline">Logout</span>
          </Button>
        </div>
      </div>
    </header>
  );
}
