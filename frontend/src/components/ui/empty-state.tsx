import { SearchX, Sparkles, Users, BookOpen, TrendingUp } from "lucide-react";
import { Card } from "@/components/ui/card";

export function EmptyState({
  title,
  description,
  icon: Icon = SearchX,
  action,
  variant = "default"
}: {
  title: string;
  description: string;
  icon?: React.ElementType;
  action?: React.ReactNode;
  variant?: "default" | "demo" | "success";
}) {
  const getVariantStyles = () => {
    switch (variant) {
      case "demo":
        return {
          iconBg: "bg-gradient-to-br from-brand-50 to-brand-100",
          iconColor: "text-brand-700",
          titleColor: "text-slate-950",
          descriptionColor: "text-slate-600"
        };
      case "success":
        return {
          iconBg: "bg-gradient-to-br from-emerald-50 to-emerald-100",
          iconColor: "text-emerald-700",
          titleColor: "text-slate-950",
          descriptionColor: "text-slate-600"
        };
      default:
        return {
          iconBg: "bg-slate-100",
          iconColor: "text-slate-500",
          titleColor: "text-slate-950",
          descriptionColor: "text-slate-500"
        };
    }
  };

  const styles = getVariantStyles();

  return (
    <Card className="p-10 text-center">
      <div className={`mx-auto flex h-16 w-16 items-center justify-center rounded-2xl ${styles.iconBg} ${styles.iconColor}`}>
        <Icon className="h-7 w-7" />
      </div>
      <h3 className={`mt-5 text-xl font-semibold ${styles.titleColor}`}>{title}</h3>
      <p className={`mx-auto mt-3 max-w-xl text-sm leading-7 ${styles.descriptionColor}`}>{description}</p>
      {action ? <div className="mt-6">{action}</div> : null}
    </Card>
  );
}

// Demo-specific empty states
export function DemoEmptyState({
  feature,
  description,
  demoAction
}: {
  feature: string;
  description: string;
  demoAction?: React.ReactNode;
}) {
  return (
    <EmptyState
      variant="demo"
      icon={Sparkles}
      title={`Explore ${feature} Features`}
      description={description}
      action={
        <div className="space-y-3">
          {demoAction}
          <p className="text-xs text-slate-500">
            💡 This demo includes sample data to showcase all features
          </p>
        </div>
      }
    />
  );
}

export function RoleDemoCTA({ role }: { role: string }) {
  const roleConfig = {
    teacher: {
      icon: BookOpen,
      title: "Experience Teacher Dashboard",
      description: "Manage classes, mark attendance, grade assignments, track student progress",
      credentials: "Demo: sarah.johnson@school.com / Teacher@123"
    },
    student: {
      icon: Users,
      title: "Explore Student Portal",
      description: "View grades, check attendance, submit assignments, track academic progress",
      credentials: "Demo: alex.martinez@student.school.com / Student@123"
    },
    parent: {
      icon: TrendingUp,
      title: "Try Parent Portal",
      description: "Monitor child's academic performance, pay fees, communicate with teachers",
      credentials: "Demo: robert.martinez@email.com / Parent@123"
    }
  };

  const config = roleConfig[role as keyof typeof roleConfig];
  if (!config) return null;

  const Icon = config.icon;

  return (
    <Card className="p-6 bg-gradient-to-br from-brand-50 to-brand-100 border-brand-200">
      <div className="flex items-start gap-4">
        <div className="shrink-0 rounded-xl bg-brand-100 p-3 text-brand-700">
          <Icon className="h-5 w-5" />
        </div>
        <div className="flex-1 text-left">
          <h4 className="font-semibold text-slate-950">{config.title}</h4>
          <p className="mt-1 text-sm text-slate-600">{config.description}</p>
          <p className="mt-3 text-xs font-medium text-brand-700 bg-brand-50 px-3 py-2 rounded-lg">
            {config.credentials}
          </p>
        </div>
      </div>
    </Card>
  );
}
