export interface Notification {
  id: string;
  userId: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  studentId?: string | null;
  studentName?: string | null;
}

export interface MarkReadDto {
  isRead: boolean;
}

export interface NotificationFilterParams {
  pageNumber?: number;
  pageSize?: number;
  unreadOnly?: boolean;
  /** When set, returns only notifications linked to this student. Used by parents for per-child filtering. */
  studentId?: string;
}

export interface SendNotificationRequest {
  title: string;
  message: string;
  userId?: string;
  roleName?: "Admin" | "Teacher" | "Student" | "Parent";
}

export interface UnreadCountResponse {
  count: number;
}
