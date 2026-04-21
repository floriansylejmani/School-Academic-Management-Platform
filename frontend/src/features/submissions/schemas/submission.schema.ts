import { z } from "zod";

const nullableString = (maxLength: number) =>
  z
    .string()
    .max(maxLength, `Maximum ${maxLength} characters`)
    .optional()
    .or(z.literal(""))
    .transform((value) => (value && value.trim().length > 0 ? value.trim() : null));

const nullableNumber = z.preprocess((value) => {
  if (value === "" || value === null || typeof value === "undefined") {
    return null;
  }

  if (typeof value === "number") {
    return Number.isNaN(value) ? null : value;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? value : parsed;
}, z.number({ invalid_type_error: "Score must be a number" }).min(0, "Score cannot be negative").nullable());

export const createSubmissionSchema = z.object({
  examId: z.string().min(1, "Exam is required"),
  essayPrompt: nullableString(2000),
  answerText: z.string().trim().min(1, "Answer is required").max(20000, "Maximum 20000 characters")
});

export const requestSubmissionAISchema = z.object({
  rubricInstructions: nullableString(2000),
  additionalInstructions: nullableString(1000)
});

export const teacherReviewSchema = z.object({
  teacherFinalScore: nullableNumber,
  teacherFinalGrade: nullableString(20),
  teacherReviewNotes: nullableString(2000),
  isAiFeedbackReleasedToStudent: z.boolean()
});

export type CreateSubmissionFormValues = z.infer<typeof createSubmissionSchema>;
export type RequestSubmissionAIFormValues = z.infer<typeof requestSubmissionAISchema>;
export type TeacherReviewFormValues = z.infer<typeof teacherReviewSchema>;
