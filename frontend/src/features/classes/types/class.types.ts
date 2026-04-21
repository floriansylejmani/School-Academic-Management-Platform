export interface AcademicClass {
  id: string;
  name: string;
  section: string;
  academicYear: string;
  classTeacherId?: string | null;
  classTeacherName?: string | null;
  studentCount: number;
  createdAt: string;
}

export interface CreateAcademicClassDto {
  name: string;
  section: string;
  academicYear: string;
  classTeacherId?: string | null;
}

export interface UpdateAcademicClassDto {
  name: string;
  section: string;
  academicYear: string;
  classTeacherId?: string | null;
}
