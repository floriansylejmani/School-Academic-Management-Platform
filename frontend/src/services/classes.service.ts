import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse, PaginationRequest } from "@/types/common";
import type { AcademicClass, CreateAcademicClassDto, UpdateAcademicClassDto } from "@/features/classes/types/class.types";

export const classesService = {
  async getAll(params?: PaginationRequest) {
    const response = await apiClient.get<ApiResponse<PagedResponse<AcademicClass>>>("/classes", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<AcademicClass>>(`/classes/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateAcademicClassDto) {
    const response = await apiClient.post<ApiResponse<AcademicClass>>("/classes", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateAcademicClassDto) {
    const response = await apiClient.put<ApiResponse<AcademicClass>>(`/classes/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/classes/${id}`);
    return response.data;
  }
};
