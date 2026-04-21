"use client";

import { NotificationsInbox } from "@/features/notifications/components/notifications-inbox";

export function StudentNotificationsClient() {
  return (
    <NotificationsInbox
      eyebrow="Notifications"
      title="Inbox"
      description="Important updates about your attendance, assessments, fees, and academic results."
      emptyTitle="No notifications yet"
      emptyDescription="You have no notifications at this time. Check back after your next class or assessment."
    />
  );
}
