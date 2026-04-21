import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type { Exam, CreateExamDto, UpdateExamDto, ExamFilterParams } from "@/features/exams/types/exams.types";

export const examsService = {
  async getAll(params?: ExamFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Exam>>>("/exams", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Exam>>(`/exams/${id}`);
    return requireApiData(response.data.data);
  },

  async getByClassId(classId: string) {
    const response = await apiClient.get<ApiResponse<Exam[]>>(`/exams/class/${classId}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateExamDto) {
    const response = await apiClient.post<ApiResponse<Exam>>("/exams", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateExamDto) {
    const response = await apiClient.put<ApiResponse<Exam>>(`/exams/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/exams/${id}`);
    return response.data;
  }
};
