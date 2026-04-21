export interface Exam {
  id: string;
  title: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  examDate: string;
  totalMarks: number;
  createdAt: string;
}

export interface CreateExamDto {
  title: string;
  classId: string;
  subjectId: string;
  examDate: string;
  totalMarks: number;
}

export interface UpdateExamDto {
  title: string;
  classId: string;
  subjectId: string;
  examDate: string;
  totalMarks: number;
}

export interface ExamFilterParams {
  classId?: string;
  subjectId?: string;
  pageNumber?: number;
  pageSize?: number;
}
