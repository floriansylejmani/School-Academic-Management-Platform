"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { parentsService } from "@/services/parents.service";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateParentDto, UpdateParentDto } from "@/features/parents/types/parents.types";

export const parentsQueryKey = ["parents"] as const;

export function useParents() {
  return useQuery({
    queryKey: parentsQueryKey,
    queryFn: () => parentsService.getAll({ pageNumber: 1, pageSize: 100 })
  });
}

export function useCreateParent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateParentDto) => parentsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: parentsQueryKey });
      toast.success("Parent created", "The parent record was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create parent", getApiErrorMessage(error));
    }
  });
}

export function useUpdateParent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateParentDto }) => parentsService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: parentsQueryKey });
      toast.success("Parent updated", "The parent record was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update parent", getApiErrorMessage(error));
    }
  });
}

export function useDeleteParent() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => parentsService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: parentsQueryKey });
      toast.success("Parent deleted", "The parent record was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete parent", getApiErrorMessage(error));
    }
  });
}
