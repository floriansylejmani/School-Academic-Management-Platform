import * as React from "react";
import { X } from "lucide-react";
import { cn } from "@/utils/cn";

export type BadgeVariant = "default" | "success" | "warning" | "danger" | "info" | "neutral";
export type BadgeSize = "sm" | "md" | "lg";

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  size?: BadgeSize;
  className?: string;
  dismissible?: boolean;
  onDismiss?: () => void;
}

const variantClasses: Record<BadgeVariant, string> = {
  default: "bg-brand-50 text-brand-700 border-brand-200",
  success: "bg-emerald-50 text-emerald-700 border-emerald-200",
  warning: "bg-amber-50 text-amber-700 border-amber-200",
  danger: "bg-rose-50 text-rose-700 border-rose-200",
  info: "bg-sky-50 text-sky-700 border-sky-200",
  neutral: "bg-slate-100 text-slate-600 border-slate-200"
};

const sizeClasses: Record<BadgeSize, string> = {
  sm: "px-2 py-0.5 text-xs",
  md: "px-2.5 py-0.5 text-xs",
  lg: "px-3 py-1 text-sm"
};

const dismissibleSizeClasses: Record<BadgeSize, string> = {
  sm: "h-3.5 w-3.5",
  md: "h-4 w-4",
  lg: "h-4 w-4"
};

export function Badge({
  children,
  variant = "default",
  size = "md",
  className,
  dismissible = false,
  onDismiss
}: BadgeProps) {
  const [isVisible, setIsVisible] = React.useState(true);

  const handleDismiss = () => {
    setIsVisible(false);
    onDismiss?.();
  };

  if (!isVisible) {
    return null;
  }

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border font-medium transition-all duration-200",
        "animate-in fade-in slide-in-from-2 zoom-in-95",
        variantClasses[variant],
        sizeClasses[size],
        className
      )}
    >
      {children}
      {dismissible && (
        <button
          type="button"
          onClick={handleDismiss}
          className={cn(
            "ml-1 rounded-full transition-colors duration-200",
            "hover:bg-current/10 focus:outline-none focus:ring-2 focus:ring-current/20",
            dismissibleSizeClasses[size]
          )}
          aria-label="Dismiss"
        >
          <X className="h-full w-full" />
        </button>
      )}
    </span>
  );
}
