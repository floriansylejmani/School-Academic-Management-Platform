"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { notificationsQueryKey, unreadCountQueryKey } from "@/features/notifications/hooks/use-notifications";
import type { UnreadCountResponse } from "@/features/notifications/types/notifications.types";
import { attendanceRealtimeService } from "@/services/realtime/attendance-realtime.service";
import { notificationRealtimeService } from "@/services/realtime/notification-realtime.service";
import { useAuthStore } from "@/store/auth.store";
import { useRealtimeStore } from "@/store/realtime.store";

export function RealtimeBridge() {
  const queryClient = useQueryClient();
  const { hasInitialized, isAuthenticated, user } = useAuthStore();
  const resetRealtime = useRealtimeStore((state) => state.reset);

  useEffect(() => {
    if (!hasInitialized) {
      return;
    }

    if (!isAuthenticated || !user) {
      void attendanceRealtimeService.stop();
      void notificationRealtimeService.stop();
      resetRealtime();
      return;
    }

    const invalidateAttendanceQueries = (studentId?: string) => {
      void queryClient.invalidateQueries({ queryKey: ["attendance"] });
      void queryClient.invalidateQueries({ queryKey: ["portal", "parent", "overview"] });

      if (studentId) {
        void queryClient.invalidateQueries({ queryKey: ["portal", "student", "attendance", studentId] });
        void queryClient.invalidateQueries({ queryKey: ["portal", "parent", "attendance", studentId] });
      }
    };

    const invalidateNotificationQueries = () => {
      void queryClient.invalidateQueries({ queryKey: notificationsQueryKey });
    };

    const unsubscribeAttendance = attendanceRealtimeService.subscribe((event) => {
      invalidateAttendanceQueries(event.attendance.studentId);
    });

    const unsubscribeAttendanceReconnect = attendanceRealtimeService.onReconnected(() => {
      invalidateAttendanceQueries();
    });

    const unsubscribeNotifications = notificationRealtimeService.subscribe((event) => {
      queryClient.setQueryData<UnreadCountResponse>(unreadCountQueryKey, {
        count: event.unreadCount
      });
      invalidateNotificationQueries();
    });

    const unsubscribeNotificationReconnect = notificationRealtimeService.onReconnected(() => {
      invalidateNotificationQueries();
      void queryClient.invalidateQueries({ queryKey: unreadCountQueryKey });
    });

    void attendanceRealtimeService.start().catch(() => undefined);
    void notificationRealtimeService.start().catch(() => undefined);

    return () => {
      unsubscribeAttendance();
      unsubscribeAttendanceReconnect();
      unsubscribeNotifications();
      unsubscribeNotificationReconnect();
      void attendanceRealtimeService.stop();
      void notificationRealtimeService.stop();
    };
  }, [hasInitialized, isAuthenticated, queryClient, resetRealtime, user]);

  return null;
}
