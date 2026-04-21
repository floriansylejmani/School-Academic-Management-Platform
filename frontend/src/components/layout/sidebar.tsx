"use client";

import Link from "next/link";
import { X } from "lucide-react";
import { usePathname } from "next/navigation";
import { LogoMark } from "@/components/layout/logo-mark";
import { useAuthStore } from "@/store/auth.store";
import { useUiStore } from "@/store/ui.store";
import { cn } from "@/utils/cn";
import { navigationItems } from "@/utils/navigation";

export function Sidebar() {
  const pathname = usePathname();
  const { user } = useAuthStore();
  const { isSidebarOpen, closeSidebar } = useUiStore();

  const allowedItems = navigationItems.filter((item) => (user ? item.roles.includes(user.role) : false));

  return (
    <>
      <div
        className={cn(
          "fixed inset-0 z-30 bg-slate-950/40 transition lg:hidden",
          isSidebarOpen ? "pointer-events-auto opacity-100" : "pointer-events-none opacity-0"
        )}
        onClick={closeSidebar}
      />
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 flex w-[min(20rem,calc(100vw-1rem))] flex-col border-r border-slate-200 bg-white px-5 py-5 transition lg:static lg:z-0 lg:w-[300px] lg:translate-x-0",
          isSidebarOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <div className="flex items-center justify-between">
          <LogoMark />
          <button
            type="button"
            onClick={closeSidebar}
            className="inline-flex h-10 w-10 items-center justify-center rounded-2xl border border-slate-200 text-slate-700 lg:hidden"
            aria-label="Close sidebar"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="mt-8 rounded-2xl bg-dashboard-glow p-5">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">Logged in as</p>
          <p className="mt-2 text-lg font-semibold text-slate-900">{user?.fullName}</p>
          <p className="mt-1 text-sm text-slate-500">{user?.email}</p>
        </div>

        <nav className="mt-8 flex-1 space-y-2">
          {allowedItems.map((item) => {
            const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);
            const Icon = item.icon;

            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={closeSidebar}
                className={cn(
                  "flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-medium transition",
                  isActive
                    ? "bg-brand-600 text-white shadow-lg shadow-brand-600/20"
                    : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                )}
              >
                <Icon className="h-4 w-4" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>

        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
          <p className="text-sm font-semibold text-slate-900">All systems live</p>
          <p className="mt-2 text-sm leading-6 text-slate-500">
            Data is scoped to your access level and updates in real time.
          </p>
        </div>
      </aside>
    </>
  );
}
