"use client";

import { useEffect } from "react";
import { CircleAlert, CircleCheck, X } from "lucide-react";
import { useToastStore } from "@/store/toast.store";
import { cn } from "@/utils/cn";

const variantStyles = {
  success: {
    icon: CircleCheck,
    className: "border-emerald-200 bg-emerald-50 text-emerald-900"
  },
  error: {
    icon: CircleAlert,
    className: "border-rose-200 bg-rose-50 text-rose-900"
  }
} as const;

export function ToastViewport() {
  const toasts = useToastStore((state) => state.toasts);
  const dismissToast = useToastStore((state) => state.dismissToast);

  useEffect(() => {
    const timers = toasts.map((toast) =>
      window.setTimeout(() => {
        dismissToast(toast.id);
      }, 3500)
    );

    return () => {
      timers.forEach((timer) => window.clearTimeout(timer));
    };
  }, [dismissToast, toasts]);

  return (
    <div className="pointer-events-none fixed right-4 top-4 z-[80] flex w-full max-w-sm flex-col gap-3">
      {toasts.map((toast) => {
        const style = variantStyles[toast.variant];
        const Icon = style.icon;

        return (
          <div
            key={toast.id}
            className={cn(
              "animate-toast-in pointer-events-auto rounded-3xl border px-4 py-4 shadow-panel backdrop-blur",
              style.className
            )}
          >
            <div className="flex items-start gap-3">
              <Icon className="mt-0.5 h-5 w-5 shrink-0" />
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold">{toast.title}</p>
                {toast.description ? <p className="mt-1 text-sm opacity-80">{toast.description}</p> : null}
              </div>
              <button
                type="button"
                onClick={() => dismissToast(toast.id)}
                className="rounded-full p-1 opacity-60 transition hover:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-current focus-visible:ring-offset-1"
                aria-label="Dismiss notification"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
