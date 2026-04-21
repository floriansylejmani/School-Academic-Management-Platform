export type DayOfWeek = "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday" | "Sunday";

export interface TimetableEntry {
  id: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  roomNumber?: string | null;
  createdAt: string;
}

export interface CreateTimetableEntryDto {
  classId: string;
  subjectId: string;
  teacherId: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  roomNumber?: string | null;
}

export interface UpdateTimetableEntryDto {
  classId: string;
  subjectId: string;
  teacherId: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  roomNumber?: string | null;
}

export interface TimetableFilterParams {
  classId?: string;
  teacherId?: string;
  dayOfWeek?: DayOfWeek;
  pageNumber?: number;
  pageSize?: number;
}
