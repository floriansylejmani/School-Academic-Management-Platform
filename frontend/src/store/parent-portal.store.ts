"use client";

import { create } from "zustand";

interface ParentPortalState {
  selectedChildId: string;
  setSelectedChildId: (childId: string) => void;
  clearSelectedChildId: () => void;
}

export const useParentPortalStore = create<ParentPortalState>((set) => ({
  selectedChildId: "",
  setSelectedChildId: (childId) => set({ selectedChildId: childId }),
  clearSelectedChildId: () => set({ selectedChildId: "" })
}));
