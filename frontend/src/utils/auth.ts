import type { Route } from "next";
import type { UserRole } from "@/types/auth";

export function getRoleDashboardPath(role: UserRole): Route {
  switch (role) {
    case "Admin":
      return "/admin/dashboard";
    case "Teacher":
      return "/teacher/dashboard";
    case "Student":
      return "/student/dashboard";
    case "Parent":
      return "/parent/dashboard";
    default:
      return "/login";
  }
}

export function getRoleFromPath(pathname: string): UserRole | null {
  if (pathname.startsWith("/admin")) return "Admin";
  if (pathname.startsWith("/teacher")) return "Teacher";
  if (pathname.startsWith("/student")) return "Student";
  if (pathname.startsWith("/parent")) return "Parent";
  return null;
}
