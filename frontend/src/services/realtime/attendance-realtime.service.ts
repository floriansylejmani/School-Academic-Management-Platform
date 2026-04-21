"use client";

import type { AttendanceRealtimeEvent } from "@/features/attendance/types/attendance-realtime.types";
import { RealtimeConnectionService } from "@/services/realtime/realtime-connection";

const attendanceConnection = new RealtimeConnectionService({
  name: "attendance",
  hubPath: "/hubs/attendance"
});

export const attendanceRealtimeService = {
  start: () => attendanceConnection.start(),
  stop: () => attendanceConnection.stop(),
  onReconnected: (handler: () => void) => attendanceConnection.onReconnected(handler),
  subscribe: (handler: (payload: AttendanceRealtimeEvent) => void) =>
    attendanceConnection.subscribe<AttendanceRealtimeEvent>("attendanceChanged", handler)
};
