import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse, PaginationRequest } from "@/types/common";
import type { CreateStudentDto, Student, UpdateStudentDto } from "@/features/students/types/student.types";

export const studentsService = {
  async getAll(params?: PaginationRequest) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Student>>>("/students", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Student>>(`/students/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateStudentDto) {
    const response = await apiClient.post<ApiResponse<Student>>("/students", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateStudentDto) {
    const response = await apiClient.put<ApiResponse<Student>>(`/students/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/students/${id}`);
    return response.data;
  }
};
