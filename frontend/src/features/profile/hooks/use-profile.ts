"use client";

import { useQuery } from "@tanstack/react-query";
import { profileService } from "@/services/profile.service";

export const teacherProfileQueryKey = ["profile", "teacher"] as const;
export const studentProfileQueryKey = ["profile", "student"] as const;

export function useTeacherProfile() {
  return useQuery({
    queryKey: teacherProfileQueryKey,
    queryFn: () => profileService.getTeacherProfile(),
    staleTime: 5 * 60 * 1000
  });
}

export function useStudentProfile() {
  return useQuery({
    queryKey: studentProfileQueryKey,
    queryFn: () => profileService.getStudentProfile(),
    staleTime: 5 * 60 * 1000
  });
}

export function useParentChildren(parentUserId: string | undefined) {
  return useQuery({
    queryKey: ["profile", "parent", "children", parentUserId],
    queryFn: () => profileService.getParentChildren(),
    enabled: Boolean(parentUserId),
    staleTime: 5 * 60 * 1000
  });
}
