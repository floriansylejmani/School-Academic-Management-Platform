export interface Teacher {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone?: string | null;
  teacherCode: string;
  specialization: string;
  hireDate: string;
  createdAt: string;
}

export interface CreateTeacherDto {
  fullName: string;
  email: string;
  password: string;
  phone?: string | null;
  address?: string | null;
  teacherCode: string;
  specialization: string;
  hireDate: string;
}

export interface UpdateTeacherDto {
  fullName: string;
  email: string;
  phone?: string | null;
  address?: string | null;
  teacherCode: string;
  specialization: string;
  hireDate: string;
}
