"use client";

import axios from "axios";
import { create } from "zustand";
import type { AuthUser } from "@/types/auth";

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  hasInitialized: boolean;
  initializeSession: (user: AuthUser | null) => void;
  hydrateSession: () => Promise<AuthUser | null>;
  refreshSession: () => Promise<void>;
  clearSession: () => void;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()((set) => ({
  user: null,
  isAuthenticated: false,
  hasInitialized: false,
  initializeSession: (user) =>
    set({
      user,
      isAuthenticated: Boolean(user),
      hasInitialized: true
    }),
  hydrateSession: async () => {
    const { authService } = await import("@/services/auth.service");

    try {
      const user = await authService.getSession();
      set({
        user,
        isAuthenticated: true,
        hasInitialized: true
      });
      return user;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 401) {
        try {
          const user = await authService.refresh();
          set({
            user,
            isAuthenticated: true,
            hasInitialized: true
          });
          return user;
        } catch {
          set({
            user: null,
            isAuthenticated: false,
            hasInitialized: true
          });
          return null;
        }
      }

      set({
        user: null,
        isAuthenticated: false,
        hasInitialized: true
      });
      throw error;
    }
  },
  refreshSession: async () => {
    const { authService } = await import("@/services/auth.service");
    const user = await authService.refresh();

    set({
      user,
      isAuthenticated: true,
      hasInitialized: true
    });
  },
  clearSession: () =>
    set({
      user: null,
      isAuthenticated: false,
      hasInitialized: true
    }),
  logout: async () => {
    const { authService } = await import("@/services/auth.service");

    try {
      await authService.logout();
    } finally {
      set({
        user: null,
        isAuthenticated: false,
        hasInitialized: true
      });
    }
  }
}));
