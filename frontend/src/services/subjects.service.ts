import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse, PaginationRequest } from "@/types/common";
import type { CreateSubjectDto, Subject, UpdateSubjectDto } from "@/features/subjects/types/subject.types";

export const subjectsService = {
  async getAll(params?: PaginationRequest) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Subject>>>("/subjects", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Subject>>(`/subjects/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateSubjectDto) {
    const response = await apiClient.post<ApiResponse<Subject>>("/subjects", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateSubjectDto) {
    const response = await apiClient.put<ApiResponse<Subject>>(`/subjects/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/subjects/${id}`);
    return response.data;
  }
};
