"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { studentsService } from "@/services/students.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateStudentDto, UpdateStudentDto } from "@/features/students/types/student.types";

export const studentsQueryKey = ["students"] as const;

export function useStudents() {
  return useQuery({
    queryKey: studentsQueryKey,
    queryFn: () => studentsService.getAll({ pageNumber: 1, pageSize: 100 })
  });
}

export function useCreateStudent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateStudentDto) => studentsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: studentsQueryKey });
      toast.success("Student created", "The student record was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create student", getApiErrorMessage(error));
    }
  });
}

export function useUpdateStudent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateStudentDto }) => studentsService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: studentsQueryKey });
      toast.success("Student updated", "The student record was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update student", getApiErrorMessage(error));
    }
  });
}

export function useDeleteStudent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => studentsService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: studentsQueryKey });
      toast.success("Student deleted", "The student record was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete student", getApiErrorMessage(error));
    }
  });
}
