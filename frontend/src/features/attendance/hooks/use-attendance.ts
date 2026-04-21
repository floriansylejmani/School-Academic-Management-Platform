"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import { attendanceService } from "@/services/attendance.service";
import { getApiErrorMessage } from "@/utils/api";
import type {
  CreateAttendanceDto,
  UpdateAttendanceDto
} from "@/features/attendance/types/attendance.types";

export const attendanceQueryKey = ["attendance"] as const;

export function useAttendance() {
  return useQuery({
    queryKey: attendanceQueryKey,
    queryFn: () => attendanceService.getAll({ pageNumber: 1, pageSize: 1000 })
  });
}

export function useCreateAttendance(options?: { silentSuccess?: boolean; skipInvalidate?: boolean }) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateAttendanceDto) => attendanceService.create(payload),
    onSuccess: async () => {
      if (!options?.skipInvalidate) {
        await queryClient.invalidateQueries({ queryKey: attendanceQueryKey });
      }
      if (!options?.silentSuccess) {
        toast.success("Attendance saved", "The attendance record was created successfully.");
      }
    },
    onError: (error) => {
      toast.error("Unable to save attendance", getApiErrorMessage(error));
    }
  });
}

export function useUpdateAttendance(options?: { silentSuccess?: boolean; skipInvalidate?: boolean }) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAttendanceDto }) =>
      attendanceService.update(id, payload),
    onSuccess: async () => {
      if (!options?.skipInvalidate) {
        await queryClient.invalidateQueries({ queryKey: attendanceQueryKey });
      }
      if (!options?.silentSuccess) {
        toast.success("Attendance updated", "The attendance record was updated successfully.");
      }
    },
    onError: (error) => {
      toast.error("Unable to update attendance", getApiErrorMessage(error));
    }
  });
}
