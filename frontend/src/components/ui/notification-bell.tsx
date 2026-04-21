"use client";

import Link from "next/link";
import { Bell } from "lucide-react";
import { useUnreadCount } from "@/features/notifications/hooks/use-notifications";

interface NotificationBellProps {
  href: string;
}

export function NotificationBell({ href }: NotificationBellProps) {
  const { data } = useUnreadCount();
  const count = data?.count ?? 0;

  return (
    <Link
      href={href}
      className="relative inline-flex h-10 w-10 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400"
      aria-label={count > 0 ? `${count} unread notifications` : "Notifications"}
    >
      <Bell className="h-5 w-5" />
      {count > 0 ? (
        <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-600 px-1 text-[10px] font-bold text-white leading-none">
          {count > 99 ? "99+" : count}
        </span>
      ) : null}
    </Link>
  );
}
