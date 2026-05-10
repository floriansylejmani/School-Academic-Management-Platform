import axios from "axios";
import type { AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "@/store/auth.store";
import { API_BASE_URL } from "@/services/apiConfig";
import { CSRF_COOKIE_NAME, getCookieValue } from "@/utils/cookies";
import { normalizePaginationParams } from "@/utils/pagination";

export interface AppAxiosRequestConfig extends AxiosRequestConfig {
  skipAuthRefresh?: boolean;
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
  skipAuthRefresh?: boolean;
}

let refreshTokenRequest: Promise<void> | null = null;

function isUnsafeMethod(method?: string) {
  if (!method) {
    return false;
  }

  const normalized = method.toUpperCase();
  return normalized !== "GET" && normalized !== "HEAD" && normalized !== "OPTIONS";
}

function redirectToLogin() {
  if (typeof window === "undefined") {
    return;
  }

  window.location.replace("/login");
}

export const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json"
  }
});

export const apiClient = api;

api.interceptors.request.use((config) => {
  config.params = normalizePaginationParams(config.params);

  if (typeof window !== "undefined" && isUnsafeMethod(config.method)) {
    const csrfToken = getCookieValue(CSRF_COOKIE_NAME);
    if (csrfToken) {
      config.headers["X-CSRF-Token"] = csrfToken;
    }
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (typeof window === "undefined") {
      return Promise.reject(error);
    }

    const originalRequest = error.config as RetryableRequestConfig | undefined;
    const isUnauthorized = error.response?.status === 401;
    const isRefreshRequest = originalRequest?.url?.includes("/auth/refresh");
    const isAuthRequest = originalRequest?.url?.includes("/auth/");
    const hasAuthenticatedSession = Boolean(useAuthStore.getState().isAuthenticated && useAuthStore.getState().user);

    if (!originalRequest || !isUnauthorized || originalRequest._retry || isRefreshRequest || originalRequest.skipAuthRefresh || !hasAuthenticatedSession) {
      if (isUnauthorized && !isAuthRequest && hasAuthenticatedSession) {
        useAuthStore.getState().clearSession();
        redirectToLogin();
      }

      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      if (!refreshTokenRequest) {
        refreshTokenRequest = useAuthStore
          .getState()
          .refreshSession()
          .finally(() => {
            refreshTokenRequest = null;
          });
      }

      await refreshTokenRequest;
      return api(originalRequest);
    } catch (refreshError) {
      useAuthStore.getState().clearSession();
      redirectToLogin();
      return Promise.reject(refreshError);
    }
  }
);
