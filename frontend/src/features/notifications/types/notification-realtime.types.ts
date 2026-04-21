import type { Notification } from "@/features/notifications/types/notifications.types";

export type NotificationRealtimeEventType = "created" | "read" | "readAll";

export interface NotificationRealtimeEvent {
  eventType: NotificationRealtimeEventType;
  notification: Notification | null;
  notificationIds: string[];
  unreadCount: number;
  occurredAt: string;
}
