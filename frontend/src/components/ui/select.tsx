import * as React from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/utils/cn";

export interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'size' | 'prefix'> {
  placeholder?: string;
  error?: boolean;
  size?: "sm" | "md" | "lg";
  icon?: React.ReactNode;
}

const sizeClasses: Record<"sm" | "md" | "lg", { height: string; padding: string; textSize: string; iconSize: string }> = {
  sm: { height: "h-9", padding: "pl-3 pr-8", textSize: "text-sm", iconSize: "h-3.5 w-3.5" },
  md: { height: "h-11", padding: "pl-4 pr-10", textSize: "text-sm", iconSize: "h-4 w-4" },
  lg: { height: "h-13", padding: "pl-5 pr-12", textSize: "text-base", iconSize: "h-5 w-5" }
};

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, children, placeholder, error = false, size = "md", icon, ...props }, ref) => {
    const sizeConfig = sizeClasses[size];
    const hasIcon = !!icon;

    return (
      <div className="relative w-full">
        {icon && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 flex items-center pointer-events-none text-slate-400 z-10">
            {icon}
          </div>
        )}
        <select
          ref={ref}
          className={cn(
            "flex w-full appearance-none rounded-xl border border-slate-200 bg-white text-slate-900 outline-none transition-all duration-200",
            "focus:border-brand-500 focus-visible:ring-2 focus-visible:ring-brand-500/20 focus-visible:ring-offset-0",
            "hover:border-slate-300",
            "disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400 disabled:border-slate-200",
            error && "border-rose-400 focus:border-rose-500 focus-visible:ring-rose-500/20",
            sizeConfig.height,
            hasIcon ? "pl-10" : sizeConfig.padding,
            sizeConfig.textSize,
            className
          )}
          {...props}
        >
          {placeholder ? <option value="">{placeholder}</option> : null}
          {children}
        </select>
        <ChevronDown
          className={cn(
            "pointer-events-none absolute top-1/2 -translate-y-1/2 text-slate-400 transition-transform duration-200",
            "peer-focus-visible:rotate-180",
            hasIcon ? "right-8" : "right-3",
            sizeConfig.iconSize
          )}
          aria-hidden="true"
        />
      </div>
    );
  }
);

Select.displayName = "Select";
