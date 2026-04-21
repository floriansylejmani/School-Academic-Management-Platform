import * as React from "react";
import { Loader2, RefreshCw } from "lucide-react";
import { Card } from "@/components/ui/card";
import { cn } from "@/utils/cn";

type LoadingVariant = "spinner" | "skeleton" | "progress" | "dots";
type LoadingSize = "sm" | "md" | "lg";

interface LoadingStateProps {
  title?: string;
  description?: string;
  variant?: LoadingVariant;
  size?: LoadingSize;
  progress?: number;
  showRetry?: boolean;
  onRetry?: () => void;
  className?: string;
  skeletonLines?: number;
}

const sizeClasses: Record<LoadingSize, { spinner: string; text: string }> = {
  sm: { spinner: "h-6 w-6", text: "text-sm" },
  md: { spinner: "h-9 w-9", text: "text-base" },
  lg: { spinner: "h-12 w-12", text: "text-lg" }
};

const skeletonHeightClasses: Record<LoadingSize, string> = {
  sm: "h-4",
  md: "h-5",
  lg: "h-6"
};

export function LoadingState({
  title = "Loading data...",
  description = "Please wait while the latest records are fetched.",
  variant = "spinner",
  size = "md",
  progress,
  showRetry = false,
  onRetry,
  className,
  skeletonLines = 3
}: LoadingStateProps) {
  const sizeConfig = sizeClasses[size];
  const skeletonHeight = skeletonHeightClasses[size];

  const renderSpinner = () => (
    <div className={cn(
      "animate-spin rounded-full border-[3px] border-slate-200 border-t-brand-600 transition-all duration-200",
      sizeConfig.spinner
    )} />
  );

  const renderSkeleton = () => (
    <div className="w-full space-y-3">
      {Array.from({ length: skeletonLines }).map((_, i) => (
        <div
          key={i}
          className={cn(
            "rounded-lg bg-slate-200 animate-pulse",
            skeletonHeight,
            i === skeletonLines - 1 ? "w-3/4" : "w-full"
          )}
          style={{
            animationDelay: `${i * 0.1}s`
          }}
        />
      ))}
    </div>
  );

  const renderProgress = () => (
    <div className="w-full max-w-md">
      <div className="mb-2 flex justify-between text-sm">
        <span className="font-medium text-slate-700">Loading...</span>
        <span className="text-slate-500">{progress || 0}%</span>
      </div>
      <div className="h-2 w-full rounded-full bg-slate-200">
        <div
          className="h-2 rounded-full bg-brand-600 transition-all duration-500 ease-out"
          style={{ width: `${progress || 0}%` }}
        />
      </div>
    </div>
  );

  const renderDots = () => (
    <div className="flex space-x-2">
      {[0, 1, 2].map((i) => (
        <div
          key={i}
          className={cn(
            "h-2 w-2 rounded-full bg-brand-600 animate-pulse",
            size === "sm" && "h-1.5 w-1.5",
            size === "lg" && "h-3 w-3"
          )}
          style={{
            animationDelay: `${i * 0.2}s`
          }}
        />
      ))}
    </div>
  );

  const renderContent = () => {
    switch (variant) {
      case "skeleton":
        return renderSkeleton();
      case "progress":
        return renderProgress();
      case "dots":
        return renderDots();
      default:
        return renderSpinner();
    }
  };

  return (
    <Card className={cn("px-6 py-12 transition-all duration-200", className)}>
      <div className="flex flex-col items-center gap-6 text-center">
        <div className="transition-all duration-200">
          {renderContent()}
        </div>
        
        <div className="space-y-2">
          <h3 className={cn("font-semibold text-slate-950 transition-colors duration-200", sizeConfig.text)}>
            {title}
          </h3>
          {description && (
            <p className="text-sm text-slate-500 transition-colors duration-200">
              {description}
            </p>
          )}
        </div>

        {showRetry && onRetry && (
          <button
            onClick={onRetry}
            className={cn(
              "inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium text-slate-600",
              "transition-all duration-200 hover:bg-slate-100 hover:text-slate-700",
              "focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            )}
          >
            <RefreshCw className="h-4 w-4" />
            Retry
          </button>
        )}
      </div>
    </Card>
  );
}

// Skeleton component for inline loading states
interface SkeletonProps {
  className?: string;
  width?: string | number;
  height?: string | number;
  lines?: number;
  animated?: boolean;
}

export function Skeleton({ 
  className, 
  width = "100%", 
  height = "1rem", 
  lines = 1,
  animated = true 
}: SkeletonProps) {
  if (lines > 1) {
    return (
      <div className={cn("space-y-2", className)}>
        {Array.from({ length: lines }).map((_, i) => (
          <div
            key={i}
            className={cn(
              "rounded-lg bg-slate-200",
              animated && "animate-pulse",
              i === lines - 1 ? "w-3/4" : "w-full"
            )}
            style={{
              height: typeof height === "number" ? `${height}px` : height,
              animationDelay: animated ? `${i * 0.1}s` : undefined
            }}
          />
        ))}
      </div>
    );
  }

  return (
    <div
      className={cn(
        "rounded-lg bg-slate-200",
        animated && "animate-pulse",
        className
      )}
      style={{
        width: typeof width === "number" ? `${width}px` : width,
        height: typeof height === "number" ? `${height}px` : height
      }}
    />
  );
}
