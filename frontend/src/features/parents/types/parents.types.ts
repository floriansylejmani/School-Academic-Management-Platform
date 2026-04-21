export interface Parent {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone?: string | null;
  address?: string | null;
  occupation?: string | null;
  studentsCount: number;
  createdAt: string;
}

export interface CreateParentDto {
  fullName: string;
  email: string;
  password: string;
  phone?: string | null;
  address?: string | null;
  occupation?: string | null;
}

export interface UpdateParentDto {
  fullName: string;
  email: string;
  phone?: string | null;
  address?: string | null;
  occupation?: string | null;
}
