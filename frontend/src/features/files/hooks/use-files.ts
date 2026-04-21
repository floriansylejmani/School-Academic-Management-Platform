"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { filesService } from "@/services/files.service";

export const filesQueryKey = ["files"] as const;

// ── Student documents ─────────────────────────────────────────────────────────

export function useStudentDocuments(studentId: string | undefined) {
  return useQuery({
    queryKey: [...filesQueryKey, "student-documents", studentId] as const,
    queryFn: () => filesService.getStudentDocuments(studentId!),
    enabled: !!studentId,
    staleTime: 30_000
  });
}

export function useUploadStudentDocument(studentId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (file: File) => filesService.uploadStudentDocument(studentId, file),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: [...filesQueryKey, "student-documents", studentId]
      });
      toast.success("Document uploaded successfully.");
    },
    onError: (error: Error) => {
      toast.error(error.message ?? "Failed to upload document.");
    }
  });
}

export function useDeleteStudentDocument(studentId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (documentId: string) =>
      filesService.deleteStudentDocument(studentId, documentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: [...filesQueryKey, "student-documents", studentId]
      });
      toast.success("Document deleted.");
    },
    onError: (error: Error) => {
      toast.error(error.message ?? "Failed to delete document.");
    }
  });
}

// ── Profile picture ───────────────────────────────────────────────────────────

export function useUploadProfilePicture(onSuccess?: (url: string) => void) {
  const toast = useToast();

  return useMutation({
    mutationFn: ({ file, userId }: { file: File; userId?: string }) =>
      filesService.uploadProfilePicture(file, userId),
    onSuccess: (data) => {
      toast.success("Profile picture updated.");
      onSuccess?.(data.downloadUrl);
    },
    onError: (error: Error) => {
      toast.error(error.message ?? "Failed to upload profile picture.");
    }
  });
}
