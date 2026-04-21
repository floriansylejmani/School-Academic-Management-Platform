"use client";

import { useToastStore } from "@/store/toast.store";

export function useToast() {
  const pushToast = useToastStore((state) => state.pushToast);

  return {
    success: (title: string, description?: string) =>
      pushToast({ title, description, variant: "success" }),
    error: (title: string, description?: string) =>
      pushToast({ title, description, variant: "error" })
  };
}
