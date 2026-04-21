import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type {
  CreateSubmissionDto,
  RequestSubmissionAIDto,
  Submission,
  SubmissionFilterParams,
  UpdateSubmissionTeacherReviewDto
} from "@/features/submissions/types/submissions.types";

export const submissionsService = {
  async getAll(params?: SubmissionFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<Submission>>>("/submissions", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<Submission>>(`/submissions/${id}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateSubmissionDto) {
    const response = await apiClient.post<ApiResponse<Submission>>("/submissions", payload);
    return requireApiData(response.data.data);
  },

  async requestAiFeedback(id: string, payload: RequestSubmissionAIDto) {
    const response = await apiClient.post<ApiResponse<Submission>>(`/submissions/${id}/ai-feedback`, payload);
    return requireApiData(response.data.data);
  },

  async requestSmartGrade(id: string, payload: RequestSubmissionAIDto) {
    const response = await apiClient.post<ApiResponse<Submission>>(`/submissions/${id}/smart-grade`, payload);
    return requireApiData(response.data.data);
  },

  async updateTeacherReview(id: string, payload: UpdateSubmissionTeacherReviewDto) {
    const response = await apiClient.put<ApiResponse<Submission>>(`/submissions/${id}/teacher-review`, payload);
    return requireApiData(response.data.data);
  }
};
