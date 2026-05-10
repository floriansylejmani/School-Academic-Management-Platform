"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { examsService } from "@/services/exams.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import { MAX_PAGE_SIZE } from "@/utils/pagination";
import type { CreateExamDto, UpdateExamDto } from "@/features/exams/types/exams.types";

export const examsQueryKey = ["exams"] as const;

export function useExams() {
  return useQuery({
    queryKey: examsQueryKey,
    queryFn: () => examsService.getAll({ pageNumber: 1, pageSize: MAX_PAGE_SIZE })
  });
}

export function useCreateExam() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateExamDto) => examsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: examsQueryKey });
      toast.success("Exam created", "The exam was scheduled successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create exam", getApiErrorMessage(error));
    }
  });
}

export function useUpdateExam() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateExamDto }) => examsService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: examsQueryKey });
      toast.success("Exam updated", "The exam details were updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update exam", getApiErrorMessage(error));
    }
  });
}

export function useDeleteExam() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => examsService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: examsQueryKey });
      toast.success("Exam deleted", "The exam was removed from the schedule.");
    },
    onError: (error) => {
      toast.error("Unable to delete exam", getApiErrorMessage(error));
    }
  });
}
