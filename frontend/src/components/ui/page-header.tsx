import * as React from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/utils/cn";

interface BreadcrumbItem {
  label: string;
  href?: string;
}

interface PageHeaderProps {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  description?: string;
  breadcrumbs?: BreadcrumbItem[];
  actionLabel?: string;
  onAction?: () => void;
  actionDisabled?: boolean;
  size?: "sm" | "md" | "lg";
  variant?: "default" | "compact" | "centered";
  className?: string;
}

const sizeClasses = {
  sm: {
    title: "text-2xl",
    description: "text-sm",
    spacing: "gap-4"
  },
  md: {
    title: "text-3xl",
    description: "text-sm",
    spacing: "gap-5"
  },
  lg: {
    title: "text-4xl",
    description: "text-base",
    spacing: "gap-6"
  }
};

const variantClasses = {
  default: "bg-dashboard-glow rounded-2xl px-6 py-7 lg:px-8",
  compact: "border-b border-slate-200 bg-white px-6 py-6 lg:px-8",
  centered: "text-center bg-dashboard-glow rounded-2xl px-6 py-8 lg:px-8"
};

export function PageHeader({
  eyebrow,
  title,
  subtitle,
  description,
  breadcrumbs,
  actionLabel,
  onAction,
  actionDisabled,
  size = "md",
  variant = "default",
  className
}: PageHeaderProps) {
  const sizeConfig = sizeClasses[size];
  const variantConfig = variantClasses[variant];

  return (
    <div className={cn(
      "transition-all duration-200",
      variantConfig,
      sizeConfig.spacing,
      variant === "centered" ? "flex flex-col items-center text-center" : "flex flex-col lg:flex-row lg:items-end lg:justify-between",
      className
    )}>
      <div className={cn(
        "flex-1",
        variant === "centered" ? "text-center" : ""
      )}>
        {breadcrumbs && breadcrumbs.length > 0 && (
          <nav className="mb-4" aria-label="Breadcrumb">
            <ol className="flex items-center space-x-2 text-sm text-slate-500">
              {breadcrumbs.map((item, index) => (
                <li key={index} className="flex items-center">
                  {index > 0 && (
                    <span className="mx-2 text-slate-400" aria-hidden="true">/</span>
                  )}
                  {item.href ? (
                    <a 
                      href={item.href} 
                      className="hover:text-brand-600 transition-colors duration-200"
                    >
                      {item.label}
                    </a>
                  ) : (
                    <span className={cn(
                      index === breadcrumbs.length - 1 && "text-slate-900 font-medium"
                    )}>
                      {item.label}
                    </span>
                  )}
                </li>
              ))}
            </ol>
          </nav>
        )}
        
        {eyebrow && (
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700 mb-2">
            {eyebrow}
          </p>
        )}
        
        <h1 className={cn(
          "font-semibold text-slate-950 transition-colors duration-200",
          sizeConfig.title
        )}>
          {title}
        </h1>
        
        {subtitle && (
          <p className="mt-2 text-lg font-medium text-slate-700">
            {subtitle}
          </p>
        )}
        
        {description && (
          <p className={cn(
            "mt-4 max-w-3xl leading-7 text-slate-600",
            sizeConfig.description
          )}>
            {description}
          </p>
        )}
      </div>

      {actionLabel && onAction && (
        <div className={cn(
          "mt-6 lg:mt-0",
          variant === "centered" && "mt-8"
        )}>
          <Button 
            onClick={onAction} 
            disabled={actionDisabled}
            size={size === "lg" ? "lg" : "md"}
            className="shadow-sm hover:shadow-md transition-all duration-200"
          >
            {actionLabel}
          </Button>
        </div>
      )}
    </div>
  );
}
