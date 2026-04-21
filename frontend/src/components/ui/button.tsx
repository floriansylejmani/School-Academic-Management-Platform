import * as React from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/utils/cn";

type ButtonVariant = "primary" | "secondary" | "ghost" | "outline" | "danger";
type ButtonSize = "sm" | "md" | "lg";
type ButtonState = "default" | "loading" | "success" | "error";

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  state?: ButtonState;
  loading?: boolean;
  icon?: React.ReactNode;
  iconPosition?: "left" | "right";
  fullWidth?: boolean;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "bg-brand-600 text-white hover:bg-brand-700 focus-visible:ring-brand-500 shadow-sm hover:shadow-md",
  secondary:
    "bg-slate-100 text-slate-900 hover:bg-slate-200 focus-visible:ring-slate-400 shadow-sm hover:shadow-md",
  ghost:
    "bg-transparent text-slate-700 hover:bg-slate-100 focus-visible:ring-slate-400",
  outline:
    "border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 focus-visible:ring-slate-400 shadow-sm hover:shadow-md",
  danger:
    "bg-rose-600 text-white hover:bg-rose-700 focus-visible:ring-rose-500 shadow-sm hover:shadow-md"
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: "h-9 px-4 text-sm",
  md: "h-11 px-5 text-sm",
  lg: "h-12 px-6 text-base"
};

const stateClasses: Record<ButtonState, string> = {
  default: "",
  loading: "cursor-not-allowed",
  success: "bg-emerald-600 hover:bg-emerald-700 border-emerald-600",
  error: "bg-rose-600 hover:bg-rose-700 border-rose-600"
};

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ 
    className, 
    variant = "primary", 
    size = "md", 
    state = "default",
    loading = false,
    icon,
    iconPosition = "left",
    fullWidth = false,
    children,
    disabled,
    ...props 
  }, ref) => {
    const isLoading = loading || state === "loading";
    const isDisabled = disabled || isLoading;

    const renderIcon = () => {
      if (isLoading) {
        return <Loader2 className="h-4 w-4 animate-spin" />;
      }
      return icon;
    };

    const renderContent = () => {
      if (iconPosition === "right") {
        return (
          <>
            {children}
            {icon && !isLoading && <span className="ml-2">{icon}</span>}
            {isLoading && <span className="ml-2">{renderIcon()}</span>}
          </>
        );
      }
      return (
        <>
          {icon && !isLoading && <span className="mr-2">{icon}</span>}
          {isLoading && <span className="mr-2">{renderIcon()}</span>}
          {children}
        </>
      );
    };

    return (
      <button
        ref={ref}
        disabled={isDisabled}
        className={cn(
          "inline-flex items-center justify-center gap-2 rounded-xl font-semibold transition-all duration-200",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2",
          "disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:shadow-sm",
          "active:scale-95",
          sizeClasses[size],
          variantClasses[variant],
          stateClasses[state],
          fullWidth && "w-full",
          className
        )}
        {...props}
      >
        {renderContent()}
      </button>
    );
  }
);

Button.displayName = "Button";
