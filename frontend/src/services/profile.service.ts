import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type { Student } from "@/features/students/types/student.types";
import type { Teacher } from "@/features/teachers/types/teacher.types";

export const profileService = {
  async getTeacherProfile() {
    const response = await apiClient.get<ApiResponse<Teacher>>("/teachers/me");
    return requireApiData(response.data.data);
  },

  async getStudentProfile() {
    const response = await apiClient.get<ApiResponse<Student>>("/students/me");
    return requireApiData(response.data.data);
  },

  async getParentChildren() {
    const response = await apiClient.get<ApiResponse<PagedResponse<Student>>>("/students/parent/me", {
      params: { pageNumber: 1, pageSize: 50 }
    });
    return requireApiData(response.data.data);
  }
};
