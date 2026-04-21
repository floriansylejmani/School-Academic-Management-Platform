import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type {
  AttendanceRecord,
  CreateAttendanceDto,
  UpdateAttendanceDto
} from "@/features/attendance/types/attendance.types";

export interface AttendanceFilterParams {
  studentId?: string;
  classId?: string;
  teacherId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export const attendanceService = {
  async getAll(params?: AttendanceFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<AttendanceRecord>>>("/attendance", { params });
    return requireApiData(response.data.data);
  },

  async getByStudentId(studentId: string) {
    const response = await apiClient.get<ApiResponse<AttendanceRecord[]>>(`/attendance/student/${studentId}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateAttendanceDto) {
    const response = await apiClient.post<ApiResponse<AttendanceRecord>>("/attendance", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateAttendanceDto) {
    const response = await apiClient.put<ApiResponse<AttendanceRecord>>(`/attendance/${id}`, payload);
    return requireApiData(response.data.data);
  }
};
