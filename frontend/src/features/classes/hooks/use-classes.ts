"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { classesService } from "@/services/classes.service";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateAcademicClassDto, UpdateAcademicClassDto } from "@/features/classes/types/class.types";

export const classesQueryKey = ["classes"] as const;

export function useClasses() {
  return useQuery({
    queryKey: classesQueryKey,
    queryFn: () => classesService.getAll({ pageNumber: 1, pageSize: 100 })
  });
}

export function useCreateClass() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateAcademicClassDto) => classesService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: classesQueryKey });
      toast.success("Class created", "The academic class was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create class", getApiErrorMessage(error));
    }
  });
}

export function useUpdateClass() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAcademicClassDto }) => classesService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: classesQueryKey });
      toast.success("Class updated", "The academic class was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update class", getApiErrorMessage(error));
    }
  });
}

export function useDeleteClass() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => classesService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: classesQueryKey });
      toast.success("Class deleted", "The academic class was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete class", getApiErrorMessage(error));
    }
  });
}
