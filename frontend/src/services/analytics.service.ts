import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse } from "@/types/common";
import type {
  AttendanceTrendsParams,
  AttendanceTrendsResponse,
  ExamPerformanceParams,
  ExamPerformanceResponse,
  FinanceSummaryResponse,
  KpiResponse
} from "@/features/analytics/types/analytics.types";

export const analyticsService = {
  async getKpis(): Promise<KpiResponse> {
    const response = await apiClient.get<ApiResponse<KpiResponse>>("/analytics/kpis");
    return requireApiData(response.data.data);
  },

  async getAttendanceTrends(params?: AttendanceTrendsParams): Promise<AttendanceTrendsResponse> {
    const response = await apiClient.get<ApiResponse<AttendanceTrendsResponse>>(
      "/analytics/attendance-trends",
      { params }
    );
    return requireApiData(response.data.data);
  },

  async getExamPerformance(params?: ExamPerformanceParams): Promise<ExamPerformanceResponse> {
    const response = await apiClient.get<ApiResponse<ExamPerformanceResponse>>(
      "/analytics/exam-performance",
      { params }
    );
    return requireApiData(response.data.data);
  },

  async getFinanceSummary(): Promise<FinanceSummaryResponse> {
    const response = await apiClient.get<ApiResponse<FinanceSummaryResponse>>(
      "/analytics/finance-summary"
    );
    return requireApiData(response.data.data);
  }
};
