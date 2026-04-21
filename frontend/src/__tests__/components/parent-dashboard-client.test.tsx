import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ParentDashboardClient } from "@/features/parent-portal/components/parent-dashboard-client";

vi.mock("@/store/auth.store", () => ({
  useAuthStore: () => ({
    user: {
      id: "parent-user",
      fullName: "Pat Parent",
      email: "pat.parent@school.com",
      role: "Parent"
    }
  })
}));

vi.mock("@/features/profile/hooks/use-profile", () => ({
  useParentChildren: () => ({
    isLoading: false,
    isError: false,
    data: {
      items: [
        {
          id: "child-1",
          userId: "student-user-1",
          fullName: "Ava Parent",
          email: "ava@school.com",
          studentCode: "ST-401",
          dateOfBirth: "2010-01-01",
          gender: 1,
          admissionDate: "2024-09-01",
          parentId: "parent-1",
          parentName: "Pat Parent",
          classId: "class-1",
          className: "Grade 6",
          createdAt: "2026-01-01T00:00:00Z"
        },
        {
          id: "child-2",
          userId: "student-user-2",
          fullName: "Mia Parent",
          email: "mia@school.com",
          studentCode: "ST-402",
          dateOfBirth: "2011-01-01",
          gender: 2,
          admissionDate: "2024-09-01",
          parentId: "parent-1",
          parentName: "Pat Parent",
          classId: "class-2",
          className: "Grade 7",
          createdAt: "2026-01-01T00:00:00Z"
        }
      ]
    }
  })
}));

vi.mock("@/features/parent-portal/hooks/use-parent-child-selection", () => ({
  useParentChildSelection: () => ({
    activeChildId: "child-2",
    activeChild: {
      id: "child-2",
      fullName: "Mia Parent",
      classId: "class-2",
      className: "Grade 7"
    },
    setSelectedChildId: vi.fn()
  })
}));

vi.mock("@/features/parent-portal/hooks/use-parent-portal", () => ({
  useParentDashboardOverview: () => ({
    isLoading: false,
    isError: false,
    data: {
      attendanceByStudentId: {
        "child-1": [
          { status: "Present" },
          { status: "Absent" }
        ],
        "child-2": [
          { status: "Present" },
          { status: "Late" }
        ]
      },
      resultsByStudentId: {
        "child-1": [
          {
            id: "result-1",
            examTitle: "Algebra Test",
            subjectName: "Mathematics",
            marksObtained: 78,
            totalMarks: 100,
            grade: "B",
            createdAt: "2026-04-01T00:00:00Z"
          }
        ],
        "child-2": [
          {
            id: "result-2",
            examTitle: "Geometry Quiz",
            subjectName: "Mathematics",
            marksObtained: 91,
            totalMarks: 100,
            grade: "A",
            createdAt: "2026-04-02T00:00:00Z"
          }
        ]
      },
      feesByStudentId: {
        "child-1": [],
        "child-2": [
          {
            id: "fee-1",
            feeType: "Final Fee",
            amount: 120,
            dueDate: "2026-05-15",
            status: "Pending"
          }
        ]
      },
      examsByClassId: {
        "class-1": [
          {
            id: "exam-1",
            title: "Algebra Test",
            subjectName: "Mathematics",
            examDate: "2026-05-01"
          }
        ],
        "class-2": [
          {
            id: "exam-2",
            title: "Physics Oral",
            subjectName: "Physics",
            examDate: "2026-05-03"
          }
        ]
      }
    }
  })
}));

vi.mock("@/features/notifications/components/notifications-summary-card", () => ({
  NotificationsSummaryCard: ({ title }: { title: string }) => <div>{title}</div>
}));

describe("ParentDashboardClient", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-04-15T10:00:00Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("shows aggregated family metrics and selected-child details", () => {
    render(<ParentDashboardClient />);

    expect(screen.getByText("Linked children")).toBeInTheDocument();
    expect(screen.getAllByText("2")).toHaveLength(2);
    expect(screen.getByText("Family attendance")).toBeInTheDocument();
    expect(screen.getByText("75%")).toBeInTheDocument();
    expect(screen.getByText("Upcoming exams")).toBeInTheDocument();
    expect(screen.getByText("Physics Oral")).toBeInTheDocument();
    expect(screen.getByText("Geometry Quiz")).toBeInTheDocument();
    expect(screen.getByText("Final Fee")).toBeInTheDocument();
    expect(screen.getAllByText("Mia Parent - Grade 7")).toHaveLength(2);
    expect(screen.queryByText("Algebra Test")).not.toBeInTheDocument();
  });
});
