import type { Route } from "next";
import {
  Bell,
  BookCopy,
  CalendarDays,
  ClipboardCheck,
  FileBarChart2,
  FileText,
  GraduationCap,
  LayoutDashboard,
  PenLine,
  ReceiptText,
  School,
  TrendingUp,
  Users,
  Users2
} from "lucide-react";
import type { NavigationItem } from "@/types/navigation";

export const navigationItems: NavigationItem[] = [
  {
    label: "Dashboard",
    href: "/admin/dashboard",
    icon: LayoutDashboard,
    roles: ["Admin"]
  },
  {
    label: "Students",
    href: "/admin/students",
    icon: GraduationCap,
    roles: ["Admin"]
  },
  {
    label: "Teachers",
    href: "/admin/teachers",
    icon: Users,
    roles: ["Admin"]
  },
  {
    label: "Parents",
    href: "/admin/parents",
    icon: Users2,
    roles: ["Admin"]
  },
  {
    label: "Classes",
    href: "/admin/classes",
    icon: School,
    roles: ["Admin"]
  },
  {
    label: "Subjects",
    href: "/admin/subjects",
    icon: BookCopy,
    roles: ["Admin"]
  },
  {
    label: "Timetable",
    href: "/admin/timetable",
    icon: CalendarDays,
    roles: ["Admin"]
  },
  {
    label: "Attendance",
    href: "/admin/attendance",
    icon: ClipboardCheck,
    roles: ["Admin"]
  },
  {
    label: "Exams",
    href: "/admin/exams",
    icon: PenLine,
    roles: ["Admin"]
  },
  {
    label: "Results",
    href: "/admin/results",
    icon: TrendingUp,
    roles: ["Admin"]
  },
  {
    label: "Submissions",
    href: "/admin/submissions",
    icon: FileText,
    roles: ["Admin"]
  },
  {
    label: "Fees",
    href: "/admin/fees",
    icon: ReceiptText,
    roles: ["Admin"]
  },
  {
    label: "Payments",
    href: "/admin/payments",
    icon: ReceiptText,
    roles: ["Admin"]
  },
  {
    label: "Reports",
    href: "/admin/reports",
    icon: FileBarChart2,
    roles: ["Admin"]
  },
  {
    label: "Notifications",
    href: "/admin/notifications",
    icon: Bell,
    roles: ["Admin"]
  },
  {
    label: "Dashboard",
    href: "/teacher/dashboard",
    icon: LayoutDashboard,
    roles: ["Teacher"]
  },
  {
    label: "Timetable",
    href: "/teacher/timetable",
    icon: CalendarDays,
    roles: ["Teacher"]
  },
  {
    label: "Attendance",
    href: "/teacher/attendance",
    icon: ClipboardCheck,
    roles: ["Teacher"]
  },
  {
    label: "Exams",
    href: "/teacher/exams",
    icon: PenLine,
    roles: ["Teacher"]
  },
  {
    label: "Results",
    href: "/teacher/results",
    icon: TrendingUp,
    roles: ["Teacher"]
  },
  {
    label: "Submissions",
    href: "/teacher/submissions",
    icon: FileText,
    roles: ["Teacher"]
  },
  {
    label: "Notifications",
    href: "/teacher/notifications",
    icon: Bell,
    roles: ["Teacher"]
  },
  {
    label: "Dashboard",
    href: "/student/dashboard",
    icon: LayoutDashboard,
    roles: ["Student"]
  },
  {
    label: "Timetable",
    href: "/student/timetable",
    icon: CalendarDays,
    roles: ["Student"]
  },
  {
    label: "Attendance",
    href: "/student/attendance",
    icon: ClipboardCheck,
    roles: ["Student"]
  },
  {
    label: "Results",
    href: "/student/results",
    icon: TrendingUp,
    roles: ["Student"]
  },
  {
    label: "Submissions",
    href: "/student/submissions",
    icon: FileText,
    roles: ["Student"]
  },
  {
    label: "Notifications",
    href: "/student/notifications",
    icon: Bell,
    roles: ["Student"]
  },
  {
    label: "Dashboard",
    href: "/parent/dashboard",
    icon: LayoutDashboard,
    roles: ["Parent"]
  },
  {
    label: "Progress",
    href: "/parent/child-progress",
    icon: TrendingUp,
    roles: ["Parent"]
  },
  {
    label: "Fees",
    href: "/parent/fees",
    icon: ReceiptText,
    roles: ["Parent"]
  },
  {
    label: "Attendance",
    href: "/parent/attendance",
    icon: ClipboardCheck,
    roles: ["Parent"]
  },
  {
    label: "Exams",
    href: "/parent/exams",
    icon: PenLine,
    roles: ["Parent"]
  },
  {
    label: "Results",
    href: "/parent/results" as Route,
    icon: TrendingUp,
    roles: ["Parent"]
  },
  {
    label: "Notifications",
    href: "/parent/notifications",
    icon: Bell,
    roles: ["Parent"]
  }
];
