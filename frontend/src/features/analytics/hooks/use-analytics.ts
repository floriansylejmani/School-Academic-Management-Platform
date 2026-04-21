"use client";

import { useQuery } from "@tanstack/react-query";
import { analyticsService } from "@/services/analytics.service";
import type {
  AttendanceTrendsParams,
  ExamPerformanceParams
} from "@/features/analytics/types/analytics.types";

export const analyticsQueryKey = ["analytics"] as const;

export function useAnalyticsKpis() {
  return useQuery({
    queryKey: [...analyticsQueryKey, "kpis"] as const,
    queryFn: () => analyticsService.getKpis(),
    staleTime: 2 * 60 * 1000  // 2 minutes — KPIs don't need sub-second freshness
  });
}

export function useAttendanceTrends(params?: AttendanceTrendsParams) {
  return useQuery({
    queryKey: [...analyticsQueryKey, "attendance-trends", params] as const,
    queryFn: () => analyticsService.getAttendanceTrends(params),
    staleTime: 2 * 60 * 1000
  });
}

export function useExamPerformance(params?: ExamPerformanceParams) {
  return useQuery({
    queryKey: [...analyticsQueryKey, "exam-performance", params] as const,
    queryFn: () => analyticsService.getExamPerformance(params),
    staleTime: 5 * 60 * 1000  // Exam data changes infrequently
  });
}

export function useFinanceSummary() {
  return useQuery({
    queryKey: [...analyticsQueryKey, "finance-summary"] as const,
    queryFn: () => analyticsService.getFinanceSummary(),
    staleTime: 2 * 60 * 1000
  });
}
