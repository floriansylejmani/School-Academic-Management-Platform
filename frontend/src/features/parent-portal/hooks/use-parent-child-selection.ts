"use client";

import { useEffect, useMemo } from "react";
import { useParentPortalStore } from "@/store/parent-portal.store";
import type { Student } from "@/features/students/types/student.types";

export function useParentChildSelection(children: Student[]) {
  const { selectedChildId, setSelectedChildId, clearSelectedChildId } = useParentPortalStore();

  const activeChild = useMemo(
    () => children.find((child) => child.id === selectedChildId) ?? children[0],
    [children, selectedChildId]
  );

  useEffect(() => {
    if (children.length === 0) {
      clearSelectedChildId();
      return;
    }

    if (!selectedChildId || !children.some((child) => child.id === selectedChildId)) {
      setSelectedChildId(children[0].id);
    }
  }, [children, clearSelectedChildId, selectedChildId, setSelectedChildId]);

  return {
    activeChild,
    activeChildId: activeChild?.id,
    setSelectedChildId
  };
}
