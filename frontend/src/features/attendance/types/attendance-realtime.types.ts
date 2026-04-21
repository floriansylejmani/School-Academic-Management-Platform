import type { AttendanceRecord } from "@/features/attendance/types/attendance.types";

export type AttendanceRealtimeEventType = "created" | "updated";

export interface AttendanceRealtimeEvent {
  eventType: AttendanceRealtimeEventType;
  attendance: AttendanceRecord;
  occurredAt: string;
}
