import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type { Result, CreateResultDto, UpdateResultDto, ResultFilterParams } from "@/features/results/types/results.types";

export const resultsService = {
  async getAll(params?: ResultFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Result>>>("/results", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Result>>(`/results/${id}`);
    return requireApiData(response.data.data);
  },

  async getByStudentId(studentId: string) {
    const response = await apiClient.get<ApiResponse<Result[]>>(`/results/student/${studentId}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateResultDto) {
    const response = await apiClient.post<ApiResponse<Result>>("/results", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateResultDto) {
    const response = await apiClient.put<ApiResponse<Result>>(`/results/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/results/${id}`);
    return response.data;
  }
};
