import * as React from "react";
import { cn } from "@/utils/cn";

type CardVariant = "default" | "elevated" | "outlined" | "flat";
type CardSize = "sm" | "md" | "lg";

interface CardProps {
  className?: string;
  children: React.ReactNode;
  variant?: CardVariant;
  size?: CardSize;
  hover?: boolean;
  padding?: "none" | "sm" | "md" | "lg";
}

interface CardHeaderProps {
  className?: string;
  children: React.ReactNode;
  size?: "sm" | "md" | "lg";
}

interface CardBodyProps {
  className?: string;
  children: React.ReactNode;
  size?: "sm" | "md" | "lg";
}

interface CardFooterProps {
  className?: string;
  children: React.ReactNode;
  size?: "sm" | "md" | "lg";
}

const variantClasses: Record<CardVariant, string> = {
  default: "border border-slate-200 bg-white shadow-card",
  elevated: "border border-slate-200/50 bg-white shadow-panel",
  outlined: "border border-slate-300 bg-white",
  flat: "border-0 bg-slate-50"
};

const sizeClasses: Record<CardSize, string> = {
  sm: "rounded-lg",
  md: "rounded-xl",
  lg: "rounded-2xl"
};

const paddingClasses: Record<"none" | "sm" | "md" | "lg", string> = {
  none: "",
  sm: "p-4",
  md: "p-6",
  lg: "p-8"
};

const headerSizeClasses: Record<"sm" | "md" | "lg", string> = {
  sm: "px-4 pt-4 pb-2",
  md: "px-6 pt-6 pb-4",
  lg: "px-8 pt-8 pb-6"
};

const bodySizeClasses: Record<"sm" | "md" | "lg", string> = {
  sm: "px-4 pb-4",
  md: "px-6 pb-6",
  lg: "px-8 pb-8"
};

const footerSizeClasses: Record<"sm" | "md" | "lg", string> = {
  sm: "px-4 pb-4 pt-2",
  md: "px-6 pb-6 pt-4",
  lg: "px-8 pb-8 pt-6"
};

export function Card({ 
  className, 
  children, 
  variant = "default", 
  size = "md", 
  hover = false,
  padding = "md"
}: CardProps) {
  return (
    <div
      className={cn(
        "transition-all duration-200",
        variantClasses[variant],
        sizeClasses[size],
        paddingClasses[padding],
        hover && "hover:shadow-panel hover:-translate-y-0.5",
        className
      )}
    >
      {children}
    </div>
  );
}

export function CardHeader({ className, children, size = "md" }: CardHeaderProps) {
  return (
    <div className={cn("border-b border-slate-100", headerSizeClasses[size], className)}>
      {children}
    </div>
  );
}

export function CardBody({ className, children, size = "md" }: CardBodyProps) {
  return <div className={cn(bodySizeClasses[size], className)}>{children}</div>;
}

export function CardFooter({ className, children, size = "md" }: CardFooterProps) {
  return (
    <div className={cn("border-t border-slate-100", footerSizeClasses[size], className)}>
      {children}
    </div>
  );
}

// Compound component pattern
Card.Header = CardHeader;
Card.Body = CardBody;
Card.Footer = CardFooter;
