import { z } from "zod";

export const parentSchema = z.object({
  fullName: z.string().min(1, "Full name is required").max(150, "Maximum 150 characters"),
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters").optional(),
  phone: z.string().max(30, "Maximum 30 characters").optional().or(z.literal("")),
  address: z.string().max(250, "Maximum 250 characters").optional().or(z.literal("")),
  occupation: z.string().max(100, "Maximum 100 characters").optional().or(z.literal(""))
});

export type ParentFormValues = z.infer<typeof parentSchema>;
