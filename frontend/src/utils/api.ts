import axios from "axios";
import type { ApiResponse } from "@/types/common";

export function getApiErrorMessage(error: unknown, fallback = "Something went wrong.") {
  if (axios.isAxiosError<ApiResponse<unknown>>(error)) {
    const response = error.response?.data;
    const validationSummary = getApiValidationSummary(error);

    if (response?.message === "Validation failed" && validationSummary) {
      return validationSummary;
    }

    if (response?.message) {
      return response.message;
    }

    if (error.response?.status === 401) {
      return "Your session has expired. Please sign in again.";
    }

    if (error.response?.status === 403) {
      return "You do not have permission to perform this action.";
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export function getApiValidationSummary(error: unknown) {
  if (!axios.isAxiosError<ApiResponse<unknown>>(error)) {
    return undefined;
  }

  const errors = error.response?.data?.errors;
  if (!errors) {
    return undefined;
  }

  return Object.entries(errors)
    .map(([field, messages]) => `${field}: ${messages.join(", ")}`)
    .join(" | ");
}

export async function getApiErrorMessageAsync(error: unknown, fallback = "Something went wrong.") {
  if (!axios.isAxiosError(error)) {
    return getApiErrorMessage(error, fallback);
  }

  const responseData = error.response?.data;
  if (responseData instanceof Blob) {
    try {
      const text = await responseData.text();
      const parsed = JSON.parse(text) as ApiResponse<unknown>;

      if (parsed.message === "Validation failed" && parsed.errors) {
        return Object.entries(parsed.errors)
          .map(([field, messages]) => `${field}: ${messages.join(", ")}`)
          .join(" | ");
      }

      if (parsed.message) {
        return parsed.message;
      }
    } catch {
      return getApiErrorMessage(error, fallback);
    }
  }

  return getApiErrorMessage(error, fallback);
}
