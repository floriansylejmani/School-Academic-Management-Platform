import * as React from "react";
import { AlertTriangle, RefreshCw, Home, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { cn } from "@/utils/cn";

type ErrorVariant = "default" | "network" | "permission" | "not-found" | "critical";
type ErrorSize = "sm" | "md" | "lg";

interface ErrorStateProps {
  title?: string;
  description?: string;
  variant?: ErrorVariant;
  size?: ErrorSize;
  showRetry?: boolean;
  showHome?: boolean;
  showBack?: boolean;
  onRetry?: () => void;
  onHome?: () => void;
  onBack?: () => void;
  className?: string;
  error?: Error | string;
  code?: string | number;
}

const variantConfig = {
  default: {
    icon: AlertTriangle,
    iconBg: "bg-rose-50",
    iconColor: "text-rose-600",
    titleColor: "text-slate-950",
    descriptionColor: "text-slate-600"
  },
  network: {
    icon: AlertTriangle,
    iconBg: "bg-amber-50",
    iconColor: "text-amber-600",
    titleColor: "text-slate-950",
    descriptionColor: "text-slate-600"
  },
  permission: {
    icon: AlertTriangle,
    iconBg: "bg-slate-100",
    iconColor: "text-slate-600",
    titleColor: "text-slate-950",
    descriptionColor: "text-slate-600"
  },
  "not-found": {
    icon: AlertTriangle,
    iconBg: "bg-sky-50",
    iconColor: "text-sky-600",
    titleColor: "text-slate-950",
    descriptionColor: "text-slate-600"
  },
  critical: {
    icon: AlertTriangle,
    iconBg: "bg-rose-100",
    iconColor: "text-rose-700",
    titleColor: "text-rose-900",
    descriptionColor: "text-rose-700"
  }
};

const errorSizeConfig = {
  sm: {
    iconSize: "h-8 w-8",
    titleSize: "text-lg",
    descriptionSize: "text-sm"
  },
  md: {
    iconSize: "h-12 w-12",
    titleSize: "text-xl",
    descriptionSize: "text-sm"
  },
  lg: {
    iconSize: "h-16 w-16",
    titleSize: "text-2xl",
    descriptionSize: "text-base"
  }
};

const defaultMessages = {
  default: {
    title: "Something went wrong",
    description: "An unexpected error occurred. Please try again or contact support if the problem persists."
  },
  network: {
    title: "Network error",
    description: "Unable to connect to the server. Please check your internet connection and try again."
  },
  permission: {
    title: "Access denied",
    description: "You don't have permission to access this resource. Please contact your administrator if you think this is an error."
  },
  "not-found": {
    title: "Page not found",
    description: "The page you're looking for doesn't exist or has been moved."
  },
  critical: {
    title: "Critical error",
    description: "A serious error occurred. The system may be unavailable. Please try again later."
  }
};

export function ErrorState({
  title,
  description,
  variant = "default",
  size = "md",
  showRetry = true,
  showHome = false,
  showBack = false,
  onRetry,
  onHome,
  onBack,
  className,
  error,
  code
}: ErrorStateProps) {
  const config = variantConfig[variant];
  const sizeSettings = errorSizeConfig[size];
  const defaultMsg = defaultMessages[variant];
  const Icon = config.icon;

  const displayTitle = title || defaultMsg.title;
  const displayDescription = description || defaultMsg.description;
  const errorMessage = error instanceof Error ? error.message : error;

  return (
    <Card className={cn("px-6 py-12 transition-all duration-200", className)}>
      <div className="flex flex-col items-center gap-6 text-center">
        <div className={cn(
          "flex h-16 w-16 items-center justify-center rounded-2xl transition-all duration-200",
          config.iconBg,
          config.iconColor,
          sizeSettings.iconSize
        )}>
          <Icon className="h-7 w-7" />
        </div>

        <div className="space-y-3">
          <h3 className={cn(
            "font-semibold transition-colors duration-200",
            sizeSettings.titleSize,
            config.titleColor
          )}>
            {displayTitle}
          </h3>
          <p className={cn(
            "max-w-md transition-colors duration-200",
            sizeSettings.descriptionSize,
            config.descriptionColor
          )}>
            {displayDescription}
          </p>
          
          {(errorMessage || code) && (
            <div className="mt-4 rounded-lg bg-slate-50 p-3 text-left">
              {errorMessage && (
                <p className="text-xs font-mono text-slate-600">
                  {errorMessage}
                </p>
              )}
              {code && (
                <p className="text-xs font-mono text-slate-500 mt-1">
                  Error code: {code}
                </p>
              )}
            </div>
          )}
        </div>

        <div className="flex flex-wrap items-center justify-center gap-3">
          {showRetry && onRetry && (
            <Button
              onClick={onRetry}
              variant="primary"
              size="sm"
              className="shadow-sm hover:shadow-md transition-all duration-200"
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Try Again
            </Button>
          )}
          
          {showBack && onBack && (
            <Button
              onClick={onBack}
              variant="outline"
              size="sm"
              className="transition-all duration-200"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Go Back
            </Button>
          )}
          
          {showHome && onHome && (
            <Button
              onClick={onHome}
              variant="ghost"
              size="sm"
              className="transition-all duration-200"
            >
              <Home className="h-4 w-4 mr-2" />
              Home
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}

// Inline error component for form fields and smaller spaces
interface InlineErrorProps {
  message: string;
  className?: string;
  size?: "sm" | "md";
}

export function InlineError({ message, className, size = "sm" }: InlineErrorProps) {
  const sizeClasses = {
    sm: "text-xs",
    md: "text-sm"
  };

  return (
    <div className={cn(
      "flex items-center gap-2 rounded-lg bg-rose-50 px-3 py-2 text-rose-700 transition-colors duration-200",
      sizeClasses[size],
      className
    )}>
      <AlertTriangle className="h-4 w-4 flex-shrink-0" />
      <span>{message}</span>
    </div>
  );
}
