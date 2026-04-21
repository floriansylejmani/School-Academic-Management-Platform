import { z } from "zod";

export const teacherSchema = z.object({
  fullName: z.string().min(1, "Full name is required").max(150, "Maximum 150 characters"),
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters").optional(),
  phone: z.string().max(30, "Maximum 30 characters").optional().or(z.literal("")),
  address: z.string().max(250, "Maximum 250 characters").optional().or(z.literal("")),
  teacherCode: z.string().min(1, "Teacher code is required").max(50, "Maximum 50 characters"),
  specialization: z.string().min(1, "Specialization is required").max(100, "Maximum 100 characters"),
  hireDate: z.string().min(1, "Hire date is required")
});

export type TeacherFormValues = z.infer<typeof teacherSchema>;
