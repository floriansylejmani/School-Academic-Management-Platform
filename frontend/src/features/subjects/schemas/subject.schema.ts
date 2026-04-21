import { z } from "zod";

export const subjectSchema = z.object({
  name: z.string().min(1, "Subject name is required").max(100, "Maximum 100 characters"),
  code: z.string().min(1, "Subject code is required").max(30, "Maximum 30 characters"),
  description: z.string().max(500, "Maximum 500 characters").optional().or(z.literal(""))
});

export type SubjectFormValues = z.infer<typeof subjectSchema>;
