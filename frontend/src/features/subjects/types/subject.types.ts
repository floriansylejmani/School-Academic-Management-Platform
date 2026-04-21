export interface Subject {
  id: string;
  name: string;
  code: string;
  description?: string | null;
  createdAt: string;
}

export interface CreateSubjectDto {
  name: string;
  code: string;
  description?: string | null;
}

export interface UpdateSubjectDto {
  name: string;
  code: string;
  description?: string | null;
}
