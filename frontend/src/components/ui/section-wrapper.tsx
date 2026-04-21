import * as React from "react";
import { cn } from "@/utils/cn";

interface SectionWrapperProps {
  children: React.ReactNode;
  className?: string;
  size?: "sm" | "md" | "lg" | "xl";
  variant?: "default" | "narrow" | "wide" | "full";
  padding?: "none" | "sm" | "md" | "lg";
  background?: "default" | "muted" | "accent" | "gradient";
}

const sizeClasses: Record<NonNullable<SectionWrapperProps["size"]>, string> = {
  sm: "max-w-2xl",
  md: "max-w-4xl",
  lg: "max-w-6xl",
  xl: "max-w-7xl"
};

const variantClasses: Record<NonNullable<SectionWrapperProps["variant"]>, string> = {
  default: "mx-auto",
  narrow: "mx-auto lg:max-w-3xl",
  wide: "mx-auto lg:max-w-full",
  full: "w-full max-w-full"
};

const paddingClasses: Record<NonNullable<SectionWrapperProps["padding"]>, string> = {
  none: "",
  sm: "py-4 sm:py-6",
  md: "py-6 sm:py-8",
  lg: "py-8 sm:py-12"
};

const backgroundClasses: Record<NonNullable<SectionWrapperProps["background"]>, string> = {
  default: "",
  muted: "bg-slate-50",
  accent: "bg-brand-50",
  gradient: "bg-gradient-to-br from-brand-50 via-white to-slate-50"
};

export function SectionWrapper({
  children,
  className,
  size = "lg",
  variant = "default",
  padding = "md",
  background = "default"
}: SectionWrapperProps) {
  return (
    <section className={cn(
      "w-full transition-all duration-200",
      sizeClasses[size],
      variantClasses[variant],
      paddingClasses[padding],
      backgroundClasses[background],
      className
    )}>
      {children}
    </section>
  );
}

// Compound components for common patterns
interface SectionHeaderProps {
  title: string;
  subtitle?: string;
  description?: string;
  align?: "left" | "center" | "right";
  className?: string;
}

export function SectionHeader({
  title,
  subtitle,
  description,
  align = "left",
  className
}: SectionHeaderProps) {
  return (
    <div className={cn(
      "mb-8",
      align === "center" && "text-center",
      align === "right" && "text-right",
      className
    )}>
      <h2 className="text-2xl font-semibold text-slate-950 sm:text-3xl">
        {title}
      </h2>
      {subtitle && (
        <p className="mt-2 text-lg font-medium text-slate-700">
          {subtitle}
        </p>
      )}
      {description && (
        <p className="mt-4 max-w-3xl text-sm leading-7 text-slate-600">
          {description}
        </p>
      )}
    </div>
  );
}

interface SectionContentProps {
  children: React.ReactNode;
  className?: string;
  columns?: 1 | 2 | 3 | 4;
  gap?: "sm" | "md" | "lg";
}

export function SectionContent({
  children,
  className,
  columns = 1,
  gap = "md"
}: SectionContentProps) {
  const columnClasses = {
    1: "grid-cols-1",
    2: "grid-cols-1 md:grid-cols-2",
    3: "grid-cols-1 md:grid-cols-2 lg:grid-cols-3",
    4: "grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
  };

  const gapClasses = {
    sm: "gap-4",
    md: "gap-6",
    lg: "gap-8"
  };

  return (
    <div className={cn(
      columns > 1 && "grid",
      columnClasses[columns],
      gapClasses[gap],
      className
    )}>
      {children}
    </div>
  );
}

// Compound component pattern
SectionWrapper.Header = SectionHeader;
SectionWrapper.Content = SectionContent;
