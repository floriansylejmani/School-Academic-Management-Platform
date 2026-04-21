"use client";

import { useQuery } from "@tanstack/react-query";
import { attendanceService } from "@/services/attendance.service";
import { resultsService } from "@/services/results.service";
import { timetableService } from "@/services/timetable.service";
import { examsService } from "@/services/exams.service";

export function useStudentAttendance(studentId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "student", "attendance", studentId],
    queryFn: async () => ({ items: await attendanceService.getByStudentId(studentId!) }),
    enabled: Boolean(studentId)
  });
}

export function useStudentResults(studentId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "student", "results", studentId],
    queryFn: async () => ({ items: await resultsService.getByStudentId(studentId!) }),
    enabled: Boolean(studentId)
  });
}

export function useStudentTimetable(classId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "student", "timetable", classId],
    queryFn: async () => ({ items: await timetableService.getByClassId(classId!) }),
    enabled: Boolean(classId)
  });
}

export function useStudentExams(classId: string | undefined) {
  return useQuery({
    queryKey: ["portal", "student", "exams", classId],
    queryFn: async () => ({ items: await examsService.getByClassId(classId!) }),
    enabled: Boolean(classId)
  });
}
