import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse, PaginationRequest } from "@/types/common";
import type { CreateParentDto, Parent, UpdateParentDto } from "@/features/parents/types/parents.types";

export const parentsService = {
  async getAll(params?: PaginationRequest) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Parent>>>("/parents", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Parent>>(`/parents/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateParentDto) {
    const response = await apiClient.post<ApiResponse<Parent>>("/parents", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateParentDto) {
    const response = await apiClient.put<ApiResponse<Parent>>(`/parents/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/parents/${id}`);
    return response.data;
  }
};
