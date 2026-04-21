"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { teachersService } from "@/services/teachers.service";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateTeacherDto, UpdateTeacherDto } from "@/features/teachers/types/teacher.types";

export const teachersQueryKey = ["teachers"] as const;

export function useTeachers() {
  return useQuery({
    queryKey: teachersQueryKey,
    queryFn: () => teachersService.getAll({ pageNumber: 1, pageSize: 100 })
  });
}

export function useCreateTeacher() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateTeacherDto) => teachersService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: teachersQueryKey });
      toast.success("Teacher created", "The teacher record was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create teacher", getApiErrorMessage(error));
    }
  });
}

export function useUpdateTeacher() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTeacherDto }) => teachersService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: teachersQueryKey });
      toast.success("Teacher updated", "The teacher record was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update teacher", getApiErrorMessage(error));
    }
  });
}

export function useDeleteTeacher() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => teachersService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: teachersQueryKey });
      toast.success("Teacher deleted", "The teacher record was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete teacher", getApiErrorMessage(error));
    }
  });
}
