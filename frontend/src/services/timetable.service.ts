import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse, PagedResponse } from "@/types/common";
import type {
  TimetableEntry,
  CreateTimetableEntryDto,
  UpdateTimetableEntryDto,
  TimetableFilterParams
} from "@/features/timetable/types/timetable.types";

export const timetableService = {
  async getAll(params?: TimetableFilterParams) {
    const response = await apiClient.get<ApiResponse<PagedResponse<TimetableEntry>>>("/timetable", { params });
    return requireApiData(response.data.data);
  },

  async getById(id: string) {
    const response = await apiClient.get<ApiResponse<TimetableEntry>>(`/timetable/${id}`);
    return requireApiData(response.data.data);
  },

  async getByClassId(classId: string) {
    const response = await apiClient.get<ApiResponse<TimetableEntry[]>>(`/timetable/class/${classId}`);
    return requireApiData(response.data.data);
  },

  async create(payload: CreateTimetableEntryDto) {
    const response = await apiClient.post<ApiResponse<TimetableEntry>>("/timetable", payload);
    return requireApiData(response.data.data);
  },

  async update(id: string, payload: UpdateTimetableEntryDto) {
    const response = await apiClient.put<ApiResponse<TimetableEntry>>(`/timetable/${id}`, payload);
    return requireApiData(response.data.data);
  },

  async remove(id: string) {
    const response = await apiClient.delete<ApiResponse<null>>(`/timetable/${id}`);
    return response.data;
  }
};
