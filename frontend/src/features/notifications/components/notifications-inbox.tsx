"use client";

import { Bell, BellOff, CheckCheck, User } from "lucide-react";
import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
  useUnreadCount
} from "@/features/notifications/hooks/use-notifications";

type NotificationFilterMode = "all" | "unread";

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

export function NotificationsInbox({
  eyebrow,
  title,
  description,
  emptyTitle,
  emptyDescription,
  params,
  headerSlot
}: {
  eyebrow: string;
  title: string;
  description: string;
  emptyTitle: string;
  emptyDescription: string;
  params?: import("@/features/notifications/types/notifications.types").NotificationFilterParams;
  /** Optional slot rendered between the page header and the filter toolbar (e.g. child switcher) */
  headerSlot?: React.ReactNode;
}) {
  const [filterMode, setFilterMode] = useState<NotificationFilterMode>("all");
  const notificationsQuery = useNotifications({
    pageNumber: 1,
    pageSize: 50,
    unreadOnly: filterMode === "unread" ? true : undefined,
    ...params
  });
  const unreadCountQuery = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const notifications = notificationsQuery.data?.items ?? [];
  const unreadCount = unreadCountQuery.data?.count ?? notifications.filter((notification) => !notification.isRead).length;
  const hasUnreadFilter = filterMode === "unread";

  const headerDescription = useMemo(() => {
    if (unreadCount > 0) {
      return `${description} You have ${unreadCount} unread notification${unreadCount === 1 ? "" : "s"}.`;
    }

    return `${description} You are all caught up.`;
  }, [description, unreadCount]);

  if (notificationsQuery.isLoading || unreadCountQuery.isLoading) {
    return <LoadingState title="Loading notifications..." description="Fetching your latest notifications." />;
  }

  if (notificationsQuery.isError || unreadCountQuery.isError) {
    return (
      <EmptyState
        title="Unable to load notifications"
        description="Notification data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void notificationsQuery.refetch();
              void unreadCountQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={eyebrow} title={title} description={headerDescription} />

      {headerSlot}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex gap-2">
          {(["all", "unread"] as NotificationFilterMode[]).map((mode) => (
            <Button
              key={mode}
              variant={filterMode === mode ? "primary" : "ghost"}
              onClick={() => setFilterMode(mode)}
            >
              {mode === "all" ? "All" : `Unread (${unreadCount})`}
            </Button>
          ))}
        </div>

        {unreadCount > 0 ? (
          <Button
            variant="secondary"
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
          >
            <CheckCheck className="mr-2 h-4 w-4" />
            {markAllRead.isPending ? "Marking..." : "Mark all as read"}
          </Button>
        ) : null}
      </div>

      {notifications.length === 0 ? (
        <EmptyState
          title={hasUnreadFilter ? "No unread notifications" : emptyTitle}
          description={hasUnreadFilter ? "There are no unread notifications right now." : emptyDescription}
        />
      ) : (
        <div className="space-y-3">
          {notifications.map((notification) => (
            <Card
              key={notification.id}
              className={`flex items-start gap-4 p-5 transition ${notification.isRead ? "opacity-60" : ""}`}
            >
              <div
                className={`mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl ${
                  notification.isRead ? "bg-slate-100 text-slate-400" : "bg-brand-50 text-brand-700"
                }`}
              >
                {notification.isRead ? <BellOff className="h-5 w-5" /> : <Bell className="h-5 w-5" />}
              </div>

              <div className="min-w-0 flex-1">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex min-w-0 flex-col gap-1">
                    <p className={`font-semibold ${notification.isRead ? "text-slate-600" : "text-slate-900"}`}>
                      {notification.title}
                    </p>
                    {notification.studentName ? (
                      <span className="inline-flex w-fit items-center gap-1 rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-600">
                        <User className="h-3 w-3" />
                        {notification.studentName}
                      </span>
                    ) : null}
                  </div>
                  <span className="shrink-0 text-xs text-slate-400">{timeAgo(notification.createdAt)}</span>
                </div>
                <p className="mt-1 text-sm leading-6 text-slate-500">{notification.message}</p>
              </div>

              {!notification.isRead ? (
                <Button
                  variant="ghost"
                  className="h-8 shrink-0 px-3 text-xs text-brand-600"
                  onClick={() => markRead.mutate(notification.id)}
                  disabled={markRead.isPending}
                >
                  Mark read
                </Button>
              ) : null}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
