import type { ApiResponse } from "@/types/common";
import type {
  AuthUser,
  ForgotPasswordPayload,
  ForgotPasswordResponse,
  LoginPayload,
  ResetPasswordPayload
} from "@/types/auth";
import { api, type AppAxiosRequestConfig } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";

const skipAuthRefreshConfig: AppAxiosRequestConfig = {
  skipAuthRefresh: true
};

export const authService = {
  async login(payload: LoginPayload) {
    const response = await api.post<ApiResponse<AuthUser>>(
      "/auth/login",
      {
        email: payload.email,
        password: payload.password
      },
      skipAuthRefreshConfig
    );

    return requireApiData(response.data.data, "Login did not return a session.");
  },

  async getSession() {
    const response = await api.get<ApiResponse<AuthUser>>("/auth/session", skipAuthRefreshConfig);
    return requireApiData(response.data.data, "Session did not return the authenticated user.");
  },

  async refresh() {
    const response = await api.post<ApiResponse<AuthUser>>("/auth/refresh", undefined, skipAuthRefreshConfig);
    return requireApiData(response.data.data, "Refresh did not return the authenticated user.");
  },

  async logout() {
    await api.post<ApiResponse<null>>("/auth/logout", undefined, skipAuthRefreshConfig);
  },

  async forgotPassword(payload: ForgotPasswordPayload) {
    const response = await api.post<ApiResponse<ForgotPasswordResponse>>(
      "/auth/forgot-password",
      {
        email: payload.email
      },
      skipAuthRefreshConfig
    );

    return requireApiData(response.data.data, "Forgot-password did not return a response message.");
  },

  async resetPassword(payload: ResetPasswordPayload) {
    await api.post<ApiResponse<null>>(
      "/auth/reset-password",
      {
        token: payload.token,
        newPassword: payload.newPassword,
        confirmPassword: payload.confirmPassword
      },
      skipAuthRefreshConfig
    );
  }
};
