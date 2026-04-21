import { z } from "zod";

export const resultSchema = z.object({
  examId: z.string().min(1, "Exam is required"),
  studentId: z.string().min(1, "Student is required"),
  marksObtained: z
    .number({ invalid_type_error: "Marks obtained must be a number" })
    .min(0, "Marks cannot be negative")
    .max(1000, "Marks cannot exceed 1000"),
  grade: z.enum(["A+", "A", "B+", "B", "C+", "C", "D", "F"], {
    required_error: "Grade is required"
  }),
  remarks: z.string().max(500, "Maximum 500 characters").optional().or(z.literal(""))
});

export type ResultFormValues = z.infer<typeof resultSchema>;
