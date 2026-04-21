"use client";

import { useQuery } from "@tanstack/react-query";
import { attendanceService } from "@/services/attendance.service";
import { examsService } from "@/services/exams.service";
import { resultsService } from "@/services/results.service";
import { timetableService } from "@/services/timetable.service";
import { feesService } from "@/services/fees.service";
import type { Student } from "@/features/students/types/student.types";

export function useChildAttendance(studentId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "parent", "attendance", studentId],
    queryFn: async () => ({ items: await attendanceService.getByStudentId(studentId!) }),
    enabled: Boolean(studentId)
  });
}

export function useChildResults(studentId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "parent", "results", studentId],
    queryFn: async () => ({ items: await resultsService.getByStudentId(studentId!) }),
    enabled: Boolean(studentId)
  });
}

export function useChildTimetable(classId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "parent", "timetable", classId],
    queryFn: async () => ({ items: await timetableService.getByClassId(classId!) }),
    enabled: Boolean(classId)
  });
}

export function useChildFees(studentId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "parent", "fees", studentId],
    queryFn: async () => ({ items: await feesService.getByStudentId(studentId!) }),
    enabled: Boolean(studentId)
  });
}

export function useChildExams(classId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "parent", "exams", classId],
    queryFn: async () => ({ items: await examsService.getByClassId(classId!) }),
    enabled: Boolean(classId)
  });
}

export function useParentDashboardOverview(children: Student[]) {
  const studentIds = children.map((child) => child.id);
  const classIds = Array.from(
    new Set(children.map((child) => child.classId).filter((classId): classId is string => Boolean(classId)))
  );

  return useQuery({
    queryKey: ["portal", "parent", "overview", studentIds, classIds],
    enabled: children.length > 0,
    queryFn: async () => {
      const [attendanceEntries, resultEntries, feeEntries, examEntries] = await Promise.all([
        Promise.all(children.map(async (child) => [child.id, await attendanceService.getByStudentId(child.id)] as const)),
        Promise.all(children.map(async (child) => [child.id, await resultsService.getByStudentId(child.id)] as const)),
        Promise.all(children.map(async (child) => [child.id, await feesService.getByStudentId(child.id)] as const)),
        Promise.all(classIds.map(async (classId) => [classId, await examsService.getByClassId(classId)] as const))
      ]);

      return {
        attendanceByStudentId: Object.fromEntries(attendanceEntries),
        resultsByStudentId: Object.fromEntries(resultEntries),
        feesByStudentId: Object.fromEntries(feeEntries),
        examsByClassId: Object.fromEntries(examEntries)
      };
    }
  });
}
