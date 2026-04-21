export type Gender = "Male" | "Female" | "Other";
export type GenderValue = 1 | 2 | 3;

export interface Student {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone?: string | null;
  studentCode: string;
  dateOfBirth: string;
  gender: GenderValue;
  admissionDate: string;
  parentId?: string | null;
  parentName?: string | null;
  classId?: string | null;
  className?: string | null;
  createdAt: string;
}

export interface CreateStudentDto {
  fullName: string;
  email: string;
  password: string;
  phone?: string | null;
  address?: string | null;
  studentCode: string;
  dateOfBirth: string;
  gender: GenderValue;
  admissionDate: string;
  parentId?: string | null;
  classId?: string | null;
}

export interface UpdateStudentDto {
  fullName: string;
  email: string;
  phone?: string | null;
  address?: string | null;
  studentCode: string;
  dateOfBirth: string;
  gender: GenderValue;
  admissionDate: string;
  parentId?: string | null;
  classId?: string | null;
}
