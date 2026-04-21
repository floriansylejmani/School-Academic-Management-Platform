import { renderHook, waitFor, act } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentPortalStore } from "@/store/parent-portal.store";
import type { Student } from "@/features/students/types/student.types";

const children: Student[] = [
  {
    id: "child-1",
    userId: "user-1",
    fullName: "Ava Parent",
    email: "ava@school.com",
    studentCode: "ST-401",
    dateOfBirth: "2010-01-01",
    gender: 1,
    admissionDate: "2024-09-01",
    parentId: "parent-1",
    parentName: "Parent One",
    classId: "class-1",
    className: "Grade 6",
    createdAt: "2026-01-01T00:00:00Z"
  },
  {
    id: "child-2",
    userId: "user-2",
    fullName: "Mia Parent",
    email: "mia@school.com",
    studentCode: "ST-402",
    dateOfBirth: "2011-01-01",
    gender: 2,
    admissionDate: "2024-09-01",
    parentId: "parent-1",
    parentName: "Parent One",
    classId: "class-2",
    className: "Grade 7",
    createdAt: "2026-01-01T00:00:00Z"
  }
];

describe("useParentChildSelection", () => {
  beforeEach(() => {
    useParentPortalStore.setState({ selectedChildId: "" });
  });

  it("falls back to the first linked child when nothing is selected", async () => {
    const { result } = renderHook(() => useParentChildSelection(children));

    await waitFor(() => {
      expect(result.current.activeChildId).toBe("child-1");
    });

    expect(useParentPortalStore.getState().selectedChildId).toBe("child-1");
  });

  it("keeps the selected child when switching between linked children", async () => {
    const { result, rerender } = renderHook(() => useParentChildSelection(children));

    await waitFor(() => {
      expect(result.current.activeChildId).toBe("child-1");
    });

    act(() => {
      result.current.setSelectedChildId("child-2");
    });

    rerender();

    expect(result.current.activeChildId).toBe("child-2");
    expect(result.current.activeChild?.fullName).toBe("Mia Parent");
  });
});
