export type AttendanceStatus = "Present" | "Absent" | "Late" | "Excused";

export interface AttendanceRecord {
  id: string;
  studentId: string;
  studentName: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  date: string;
  status: AttendanceStatus;
  remarks?: string | null;
  createdAt: string;
}

export interface CreateAttendanceDto {
  studentId: string;
  classId: string;
  subjectId: string;
  teacherId: string;
  date: string;
  status: AttendanceStatus;
  remarks?: string | null;
}

export interface UpdateAttendanceDto {
  studentId: string;
  classId: string;
  subjectId: string;
  teacherId: string;
  date: string;
  status: AttendanceStatus;
  remarks?: string | null;
}
