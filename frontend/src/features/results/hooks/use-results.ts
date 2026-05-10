"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { resultsService } from "@/services/results.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import { MAX_PAGE_SIZE } from "@/utils/pagination";
import type { CreateResultDto, UpdateResultDto } from "@/features/results/types/results.types";

export const resultsQueryKey = ["results"] as const;

export function useResults() {
  return useQuery({
    queryKey: resultsQueryKey,
    queryFn: () => resultsService.getAll({ pageNumber: 1, pageSize: MAX_PAGE_SIZE })
  });
}

export function useCreateResult() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateResultDto) => resultsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resultsQueryKey });
      toast.success("Result recorded", "The student result was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to record result", getApiErrorMessage(error));
    }
  });
}

export function useUpdateResult() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateResultDto }) => resultsService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resultsQueryKey });
      toast.success("Result updated", "The student result was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update result", getApiErrorMessage(error));
    }
  });
}

export function useDeleteResult() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => resultsService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resultsQueryKey });
      toast.success("Result deleted", "The student result was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete result", getApiErrorMessage(error));
    }
  });
}
