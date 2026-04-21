import { z } from "zod";

export const timetableSchema = z.object({
  classId: z.string().min(1, "Class is required"),
  subjectId: z.string().min(1, "Subject is required"),
  teacherId: z.string().min(1, "Teacher is required"),
  dayOfWeek: z.enum(["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], {
    required_error: "Day of week is required"
  }),
  startTime: z.string().min(1, "Start time is required"),
  endTime: z.string().min(1, "End time is required"),
  roomNumber: z.string().max(50, "Maximum 50 characters").optional().or(z.literal(""))
});

export type TimetableFormValues = z.infer<typeof timetableSchema>;
