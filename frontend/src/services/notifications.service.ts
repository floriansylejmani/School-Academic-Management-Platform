import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type {
  Notification,
  NotificationFilterParams,
  SendNotificationRequest,
  UnreadCountResponse
} from "@/features/notifications/types/notifications.types";

export const notificationsService = {
  async getAll(params?: NotificationFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Notification>>>("/notifications", { params });
    return requireApiData(response.data.data);
  },

  async getUnreadCount() {
    const response = await apiClient.get<ApiResponse<UnreadCountResponse>>("/notifications/unread-count");
    return requireApiData(response.data.data);
  },

  async markAsRead(id: string) {
    const response = await apiClient.patch<ApiResponse<Notification>>(`/notifications/${id}/read`, {});
    return requireApiData(response.data.data);
  },

  async markAllAsRead() {
    const response = await apiClient.patch<ApiResponse<null>>("/notifications/read-all", {});
    return response.data;
  },

  async send(request: SendNotificationRequest) {
    const response = await apiClient.post<ApiResponse<null>>("/notifications/send", request);
    return response.data;
  }
};
