"use client";

import { NotificationsInbox } from "@/features/notifications/components/notifications-inbox";

export function TeacherNotificationsClient() {
  return (
    <NotificationsInbox
      eyebrow="Notifications"
      title="Inbox"
      description="Updates about your classes, assessments, attendance records, and institutional announcements."
      emptyTitle="No notifications yet"
      emptyDescription="You have no notifications at this time. Check back after your next scheduled class."
    />
  );
}
