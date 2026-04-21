import { z } from "zod";

export const forgotPasswordSchema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email address")
});

export type ForgotPasswordSchema = z.infer<typeof forgotPasswordSchema>;
