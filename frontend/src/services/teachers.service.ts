import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse, PaginationRequest } from "@/types/common";
import type { CreateTeacherDto, Teacher, UpdateTeacherDto } from "@/features/teachers/types/teacher.types";

export const teachersService = {
  async getAll(params?: PaginationRequest) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Teacher>>>("/teachers", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Teacher>>(`/teachers/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateTeacherDto) {
    const response = await apiClient.post<ApiResponse<Teacher>>("/teachers", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateTeacherDto) {
    const response = await apiClient.put<ApiResponse<Teacher>>(`/teachers/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/teachers/${id}`);
    return response.data;
  }
};
