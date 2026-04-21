export type Grade = "A+" | "A" | "B+" | "B" | "C+" | "C" | "D" | "F";

export interface Result {
  id: string;
  examId: string;
  examTitle: string;
  studentId: string;
  studentName: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  marksObtained: number;
  totalMarks: number;
  grade: Grade;
  remarks?: string | null;
  createdAt: string;
}

export interface CreateResultDto {
  examId: string;
  studentId: string;
  marksObtained: number;
  grade: Grade;
  remarks?: string | null;
}

export interface UpdateResultDto {
  examId: string;
  studentId: string;
  marksObtained: number;
  grade: Grade;
  remarks?: string | null;
}

export interface ResultFilterParams {
  classId?: string;
  studentId?: string;
  examId?: string;
  subjectId?: string;
  pageNumber?: number;
  pageSize?: number;
}
