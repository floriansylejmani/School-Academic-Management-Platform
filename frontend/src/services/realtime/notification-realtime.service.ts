"use client";

import type { NotificationRealtimeEvent } from "@/features/notifications/types/notification-realtime.types";
import { RealtimeConnectionService } from "@/services/realtime/realtime-connection";

const notificationConnection = new RealtimeConnectionService({
  name: "notifications",
  hubPath: "/hubs/notifications"
});

export const notificationRealtimeService = {
  start: () => notificationConnection.start(),
  stop: () => notificationConnection.stop(),
  onReconnected: (handler: () => void) => notificationConnection.onReconnected(handler),
  subscribe: (handler: (payload: NotificationRealtimeEvent) => void) =>
    notificationConnection.subscribe<NotificationRealtimeEvent>("notificationEvent", handler)
};
