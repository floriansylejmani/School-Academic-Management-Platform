import { z } from "zod";

export const examSchema = z.object({
  title: z.string().min(1, "Title is required").max(200, "Maximum 200 characters"),
  classId: z.string().min(1, "Class is required"),
  subjectId: z.string().min(1, "Subject is required"),
  examDate: z.string().min(1, "Exam date is required"),
  totalMarks: z
    .number({ invalid_type_error: "Total marks must be a number" })
    .int("Total marks must be a whole number")
    .min(1, "Total marks must be at least 1")
    .max(1000, "Total marks cannot exceed 1000")
});

export type ExamFormValues = z.infer<typeof examSchema>;
