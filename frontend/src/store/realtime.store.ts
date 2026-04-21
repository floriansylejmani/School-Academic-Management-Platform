"use client";

import { create } from "zustand";

export type RealtimeConnectionName = "attendance" | "notifications";
export type RealtimeConnectionStatus = "disconnected" | "connecting" | "connected" | "reconnecting";

interface RealtimeState {
  connections: Record<RealtimeConnectionName, RealtimeConnectionStatus>;
  setConnectionStatus: (name: RealtimeConnectionName, status: RealtimeConnectionStatus) => void;
  reset: () => void;
}

const initialConnections: Record<RealtimeConnectionName, RealtimeConnectionStatus> = {
  attendance: "disconnected",
  notifications: "disconnected"
};

export const useRealtimeStore = create<RealtimeState>()((set) => ({
  connections: initialConnections,
  setConnectionStatus: (name, status) =>
    set((state) => ({
      connections: {
        ...state.connections,
        [name]: status
      }
    })),
  reset: () =>
    set({
      connections: initialConnections
    })
}));
