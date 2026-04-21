"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { subjectsService } from "@/services/subjects.service";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateSubjectDto, UpdateSubjectDto } from "@/features/subjects/types/subject.types";

export const subjectsQueryKey = ["subjects"] as const;

export function useSubjects() {
  return useQuery({
    queryKey: subjectsQueryKey,
    queryFn: () => subjectsService.getAll({ pageNumber: 1, pageSize: 100 })
  });
}

export function useCreateSubject() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateSubjectDto) => subjectsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: subjectsQueryKey });
      toast.success("Subject created", "The subject was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create subject", getApiErrorMessage(error));
    }
  });
}

export function useUpdateSubject() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSubjectDto }) => subjectsService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: subjectsQueryKey });
      toast.success("Subject updated", "The subject was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update subject", getApiErrorMessage(error));
    }
  });
}

export function useDeleteSubject() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => subjectsService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: subjectsQueryKey });
      toast.success("Subject deleted", "The subject was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete subject", getApiErrorMessage(error));
    }
  });
}
