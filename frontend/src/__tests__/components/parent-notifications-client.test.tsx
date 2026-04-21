import { render, screen, fireEvent } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ParentNotificationsClient } from "@/features/parent-portal/components/parent-notifications-client";

// ── auth ──────────────────────────────────────────────────────────────────────
vi.mock("@/store/auth.store", () => ({
  useAuthStore: () => ({
    user: { id: "parent-user-1", fullName: "Pat Parent", email: "pat@school.com", role: "Parent" }
  })
}));

// ── children ──────────────────────────────────────────────────────────────────
const children = [
  {
    id: "child-1",
    userId: "student-user-1",
    fullName: "Ava Child",
    email: "ava@school.com",
    studentCode: "SC-001",
    dateOfBirth: "2012-01-01",
    gender: 1,
    admissionDate: "2024-09-01",
    parentId: "parent-1",
    parentName: "Pat Parent",
    classId: "class-1",
    className: "Grade 5",
    createdAt: "2026-01-01T00:00:00Z"
  },
  {
    id: "child-2",
    userId: "student-user-2",
    fullName: "Mia Child",
    email: "mia@school.com",
    studentCode: "SC-002",
    dateOfBirth: "2013-06-01",
    gender: 2,
    admissionDate: "2024-09-01",
    parentId: "parent-1",
    parentName: "Pat Parent",
    classId: "class-2",
    className: "Grade 4",
    createdAt: "2026-01-01T00:00:00Z"
  }
];

vi.mock("@/features/profile/hooks/use-profile", () => ({
  useParentChildren: () => ({
    isLoading: false,
    isError: false,
    data: { items: children }
  })
}));

vi.mock("@/features/parent-portal/hooks/use-parent-child-selection", () => ({
  useParentChildSelection: () => ({
    activeChildId: "child-1",
    activeChild: children[0],
    setSelectedChildId: vi.fn()
  })
}));

// ── notifications inbox ───────────────────────────────────────────────────────
type InboxParams = { studentId?: string } | undefined;

vi.mock("@/features/notifications/components/notifications-inbox", () => ({
  NotificationsInbox: (props: { title: string; params?: InboxParams; headerSlot?: React.ReactNode }) => (
    <div>
      <span data-testid="inbox-title">{props.title}</span>
      <span data-testid="inbox-student-id">{props.params?.studentId ?? "none"}</span>
      <div data-testid="header-slot">{props.headerSlot}</div>
    </div>
  )
}));

describe("ParentNotificationsClient", () => {
  it("renders in aggregated mode by default — no studentId filter", () => {
    render(<ParentNotificationsClient />);

    expect(screen.getByTestId("inbox-student-id")).toHaveTextContent("none");
    expect(screen.getByText("All children")).toBeInTheDocument();
    expect(screen.getByText("By child")).toBeInTheDocument();
  });

  it("shows the child switcher after clicking By child (multi-child parent)", () => {
    render(<ParentNotificationsClient />);

    fireEvent.click(screen.getByText("By child"));

    // ParentChildSwitcher renders a <select> for parents with > 1 child
    expect(screen.getByRole("combobox")).toBeInTheDocument();
  });

  it("passes studentId to inbox when By child mode is active", () => {
    render(<ParentNotificationsClient />);

    fireEvent.click(screen.getByText("By child"));

    expect(screen.getByTestId("inbox-student-id")).toHaveTextContent("child-1");
  });

  it("clears studentId filter when switching back to All children", () => {
    render(<ParentNotificationsClient />);

    fireEvent.click(screen.getByText("By child"));
    expect(screen.getByTestId("inbox-student-id")).toHaveTextContent("child-1");

    fireEvent.click(screen.getByText("All children"));
    expect(screen.getByTestId("inbox-student-id")).toHaveTextContent("none");
  });

});
