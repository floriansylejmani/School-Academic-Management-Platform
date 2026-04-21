import type { Route } from "next";
import type { LucideIcon } from "lucide-react";
import type { UserRole } from "@/types/auth";

export interface NavigationItem {
  label: string;
  href: Route;
  icon: LucideIcon;
  roles: UserRole[];
}
