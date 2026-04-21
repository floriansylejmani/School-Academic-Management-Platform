import { z } from "zod";

export const classSchema = z.object({
  name: z.string().min(1, "Class name is required").max(50, "Maximum 50 characters"),
  section: z.string().min(1, "Section is required").max(20, "Maximum 20 characters"),
  academicYear: z.string().min(1, "Academic year is required").max(20, "Maximum 20 characters"),
  classTeacherId: z.string().optional().or(z.literal(""))
});

export type ClassFormValues = z.infer<typeof classSchema>;
