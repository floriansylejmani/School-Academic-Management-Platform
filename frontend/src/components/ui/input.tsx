import * as React from "react";
import { cn } from "@/utils/cn";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement> & {
  prefix?: React.ReactNode;
  suffix?: React.ReactNode;
  error?: boolean;
  size?: "sm" | "md" | "lg";
};

const sizeClasses: Record<"sm" | "md" | "lg", string> = {
  sm: "h-9 px-3 text-sm",
  md: "h-11 px-4 text-sm",
  lg: "h-13 px-5 text-base"
};

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, prefix, suffix, error = false, size = "md", ...props }, ref) => {
    const hasAffix = prefix || suffix;
    
    // Get size classes explicitly to avoid TypeScript errors
    const getSizeClasses = () => {
      switch (size) {
        case "sm":
          return { height: "h-9", padding: "px-3", textSize: "text-sm" };
        case "lg":
          return { height: "h-13", padding: "px-5", textSize: "text-base" };
        default:
          return { height: "h-11", padding: "px-4", textSize: "text-sm" };
      }
    };

    const sizeConfig = getSizeClasses();
    const paddingClass = hasAffix ? "px-3" : sizeConfig.padding;

    return (
      <div className="relative w-full">
        {prefix && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 flex items-center pointer-events-none text-slate-400">
            {prefix}
          </div>
        )}
        <input
          ref={ref}
          className={cn(
            "flex w-full rounded-xl border border-slate-200 bg-white text-slate-900 outline-none ring-0 transition-all duration-200",
            "placeholder:text-slate-400",
            "focus:border-brand-500 focus-visible:ring-2 focus-visible:ring-brand-500/20 focus-visible:ring-offset-0",
            "hover:border-slate-300",
            error && "border-rose-400 focus:border-rose-500 focus-visible:ring-rose-500/20",
            "disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400 disabled:border-slate-200",
            hasAffix && prefix && "pl-10",
            hasAffix && suffix && "pr-10",
            !hasAffix && paddingClass,
            sizeConfig.height,
            sizeConfig.textSize,
            className
          )}
          {...props}
        />
        {suffix && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center pointer-events-none text-slate-400">
            {suffix}
          </div>
        )}
      </div>
    );
  }
);

Input.displayName = "Input";
