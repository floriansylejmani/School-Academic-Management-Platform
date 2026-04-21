"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { submissionsService } from "@/services/submissions.service";
import type {
  CreateSubmissionDto,
  RequestSubmissionAIDto,
  SubmissionFilterParams,
  UpdateSubmissionTeacherReviewDto
} from "@/features/submissions/types/submissions.types";
import { getApiErrorMessage } from "@/utils/api";

export const submissionsQueryKey = ["submissions"] as const;

export function useSubmissions(params?: SubmissionFilterParams) {
  return useQuery({
    queryKey: [...submissionsQueryKey, params] as const,
    queryFn: () => submissionsService.getAll(params)
  });
}

export function useSubmission(id: string | undefined) {
  return useQuery({
    queryKey: [...submissionsQueryKey, "detail", id] as const,
    queryFn: () => submissionsService.getById(id!),
    enabled: Boolean(id)
  });
}

export function useCreateSubmission() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateSubmissionDto) => submissionsService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: submissionsQueryKey });
      toast.success("Submission saved", "Your essay answer was submitted successfully.");
    },
    onError: (error) => {
      toast.error("Unable to submit answer", getApiErrorMessage(error));
    }
  });
}

export function useRequestSubmissionAiFeedback() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RequestSubmissionAIDto }) =>
      submissionsService.requestAiFeedback(id, payload),
    onSuccess: async (submission) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: submissionsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...submissionsQueryKey, "detail", submission.id] })
      ]);
      toast.success("AI feedback ready", "Structured essay feedback was generated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to generate AI feedback", getApiErrorMessage(error));
    }
  });
}

export function useRequestSubmissionSmartGrade() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RequestSubmissionAIDto }) =>
      submissionsService.requestSmartGrade(id, payload),
    onSuccess: async (submission) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: submissionsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...submissionsQueryKey, "detail", submission.id] })
      ]);
      toast.success("Smart grade ready", "AI rubric guidance was generated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to generate smart grade", getApiErrorMessage(error));
    }
  });
}

export function useUpdateSubmissionTeacherReview() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSubmissionTeacherReviewDto }) =>
      submissionsService.updateTeacherReview(id, payload),
    onSuccess: async (submission) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: submissionsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...submissionsQueryKey, "detail", submission.id] })
      ]);
      toast.success("Teacher review saved", "The final score and release status were updated.");
    },
    onError: (error) => {
      toast.error("Unable to save teacher review", getApiErrorMessage(error));
    }
  });
}
