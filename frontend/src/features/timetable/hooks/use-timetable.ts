"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { timetableService } from "@/services/timetable.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import type { CreateTimetableEntryDto, UpdateTimetableEntryDto } from "@/features/timetable/types/timetable.types";

export const timetableQueryKey = ["timetable"] as const;

export function useTimetable() {
  return useQuery({
    queryKey: timetableQueryKey,
    queryFn: () => timetableService.getAll({ pageNumber: 1, pageSize: 200 })
  });
}

export function useCreateTimetableEntry() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateTimetableEntryDto) => timetableService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableQueryKey });
      toast.success("Entry created", "The timetable entry was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create entry", getApiErrorMessage(error));
    }
  });
}

export function useUpdateTimetableEntry() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTimetableEntryDto }) =>
      timetableService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableQueryKey });
      toast.success("Entry updated", "The timetable entry was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update entry", getApiErrorMessage(error));
    }
  });
}

export function useDeleteTimetableEntry() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => timetableService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableQueryKey });
      toast.success("Entry deleted", "The timetable entry was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete entry", getApiErrorMessage(error));
    }
  });
}
