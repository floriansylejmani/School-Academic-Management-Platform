import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type {
  CreateFeeDto,
  CreatePaymentDto,
  Fee,
  FeeFilterParams,
  Payment,
  PaymentFilterParams,
  UpdateFeeDto
} from "@/features/fees/types/fees.types";

export const feesService = {
  async getAll(params?: FeeFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Fee>>>("/fees", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Fee>>(`/fees/${id}`);
    return requireApiData(response.data.data);
  },

  async getByStudentId(studentId: string) {
    const response = await apiClient.get<ApiResponse<Fee[]>>(`/fees/student/${studentId}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateFeeDto) {
    const response = await apiClient.post<ApiResponse<Fee>>("/fees", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateFeeDto) {
    const response = await apiClient.put<ApiResponse<Fee>>(`/fees/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/fees/${id}`);
    return response.data;
  },

  async getPayments(params?: PaymentFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Payment>>>("/payments", { params });
    return requireApiData(response.data.data);
  },

  async addPayment(payload: CreatePaymentDto) {
    const response = await apiClient.post<ApiResponse<Payment>>("/payments", payload);
    return requireApiData(response.data.data);
  }
};
