"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { feesService } from "@/services/fees.service";
import { useToast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/api";
import type {
  CreateFeeDto,
  CreatePaymentDto,
  FeeFilterParams,
  PaymentFilterParams,
  UpdateFeeDto
} from "@/features/fees/types/fees.types";

export const feesQueryKey = ["fees"] as const;
export const paymentsQueryKey = ["payments"] as const;

export function useFees(params?: FeeFilterParams) {
  return useQuery({
    queryKey: params ? [...feesQueryKey, params] : feesQueryKey,
    queryFn: () => feesService.getAll({ pageNumber: 1, pageSize: 100, ...params })
  });
}

export function useCreateFee() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateFeeDto) => feesService.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: feesQueryKey });
      toast.success("Fee created", "The fee record was saved successfully.");
    },
    onError: (error) => {
      toast.error("Unable to create fee", getApiErrorMessage(error));
    }
  });
}

export function useUpdateFee() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateFeeDto }) => feesService.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: feesQueryKey });
      await queryClient.invalidateQueries({ queryKey: paymentsQueryKey });
      toast.success("Fee updated", "The fee record was updated successfully.");
    },
    onError: (error) => {
      toast.error("Unable to update fee", getApiErrorMessage(error));
    }
  });
}

export function useDeleteFee() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => feesService.remove(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: feesQueryKey });
      toast.success("Fee deleted", "The fee record was removed.");
    },
    onError: (error) => {
      toast.error("Unable to delete fee", getApiErrorMessage(error));
    }
  });
}

export function usePayments(params?: PaymentFilterParams) {
  return useQuery({
    queryKey: params ? [...paymentsQueryKey, params] : paymentsQueryKey,
    queryFn: () => feesService.getPayments({ pageNumber: 1, pageSize: 100, ...params })
  });
}

export function useAddPayment() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreatePaymentDto) => feesService.addPayment(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: paymentsQueryKey });
      await queryClient.invalidateQueries({ queryKey: feesQueryKey });
      toast.success("Payment recorded", "The payment was applied to the fee.");
    },
    onError: (error) => {
      toast.error("Unable to record payment", getApiErrorMessage(error));
    }
  });
}
