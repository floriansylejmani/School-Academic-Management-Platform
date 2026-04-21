"use client";

import Link from "next/link";
import { Bell, CheckCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useMarkAllNotificationsRead, useMarkNotificationRead, useNotifications, useUnreadCount } from "@/features/notifications/hooks/use-notifications";

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const minutes = Math.floor(diff / 60000);

  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;

  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export function NotificationsSummaryCard({
  title,
  description,
  href,
  emptyMessage
}: {
  title: string;
  description: string;
  href: string;
  emptyMessage: string;
}) {
  const notificationsQuery = useNotifications({ pageNumber: 1, pageSize: 4 });
  const unreadCountQuery = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const notifications = notificationsQuery.data?.items ?? [];
  const unreadCount = unreadCountQuery.data?.count ?? notifications.filter((notification) => !notification.isRead).length;

  return (
    <Card className="p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-semibold text-slate-950">{title}</p>
          <p className="mt-1 text-sm text-slate-500">{description}</p>
        </div>
        <div className="flex items-center gap-2">
          {unreadCount > 0 ? (
            <Button
              variant="ghost"
              onClick={() => markAllRead.mutate()}
              disabled={markAllRead.isPending}
            >
              <CheckCheck className="mr-2 h-4 w-4" />
              {markAllRead.isPending ? "Marking..." : "Mark all"}
            </Button>
          ) : null}
          <Link
            href={href}
            className="inline-flex h-11 items-center justify-center rounded-2xl bg-slate-100 px-5 text-sm font-semibold text-slate-900 transition hover:bg-slate-200"
          >
            View all
          </Link>
        </div>
      </div>

      <div className="mt-5 space-y-2.5">
        {notificationsQuery.isLoading || unreadCountQuery.isLoading ? (
          <p className="py-4 text-sm text-slate-500">Loading notifications...</p>
        ) : notificationsQuery.isError || unreadCountQuery.isError ? (
          <div className="flex items-center justify-between gap-3 rounded-2xl bg-rose-50 px-4 py-3">
            <p className="text-sm text-rose-700">Notifications could not be loaded.</p>
            <Button
              variant="ghost"
              onClick={() => {
                void notificationsQuery.refetch();
                void unreadCountQuery.refetch();
              }}
            >
              Retry
            </Button>
          </div>
        ) : notifications.length === 0 ? (
          <p className="py-4 text-sm text-slate-500">{emptyMessage}</p>
        ) : (
          notifications.map((notification) => (
            <div key={notification.id} className="rounded-2xl bg-slate-50 px-4 py-3">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <Bell className={`h-4 w-4 ${notification.isRead ? "text-slate-300" : "text-brand-700"}`} />
                    <p className={`text-sm font-medium ${notification.isRead ? "text-slate-600" : "text-slate-900"}`}>
                      {notification.title}
                    </p>
                  </div>
                  <p className="mt-1.5 text-xs leading-5 text-slate-500">{notification.message}</p>
                </div>
                <div className="text-right">
                  <p className="text-xs text-slate-400">{timeAgo(notification.createdAt)}</p>
                  {!notification.isRead ? (
                    <Button
                      variant="ghost"
                      className="mt-2 h-7 px-2 text-xs text-brand-600"
                      onClick={() => markRead.mutate(notification.id)}
                      disabled={markRead.isPending}
                    >
                      Mark read
                    </Button>
                  ) : null}
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </Card>
  );
}
