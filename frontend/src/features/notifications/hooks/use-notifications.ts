"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationsService } from "@/services/notifications.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import type { NotificationFilterParams, SendNotificationRequest } from "@/features/notifications/types/notifications.types";

export const notificationsQueryKey = ["notifications"] as const;
export const unreadCountQueryKey = ["notifications", "unread-count"] as const;

export function useNotifications(params?: NotificationFilterParams) {
  return useQuery({
    queryKey: [...notificationsQueryKey, params ?? {}],
    queryFn: () => notificationsService.getAll({ pageNumber: 1, pageSize: 50, ...params })
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: unreadCountQueryKey,
    queryFn: () => notificationsService.getUnreadCount()
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => notificationsService.markAsRead(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: notificationsQueryKey });
      await queryClient.invalidateQueries({ queryKey: unreadCountQueryKey });
    },
    onError: (error) => {
      toast.error("Unable to mark as read", getApiErrorMessage(error));
    }
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: () => notificationsService.markAllAsRead(),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: notificationsQueryKey });
      await queryClient.invalidateQueries({ queryKey: unreadCountQueryKey });
      toast.success("All marked as read", "Your notifications have been cleared.");
    },
    onError: (error) => {
      toast.error("Unable to mark all as read", getApiErrorMessage(error));
    }
  });
}

export function useSendNotification() {
  const toast = useToast();

  return useMutation({
    mutationFn: (request: SendNotificationRequest) => notificationsService.send(request),
    onSuccess: () => {
      toast.success("Notification sent", "The notification was delivered successfully.");
    },
    onError: (error) => {
      toast.error("Unable to send notification", getApiErrorMessage(error));
    }
  });
}
