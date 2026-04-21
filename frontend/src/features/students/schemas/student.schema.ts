import { z } from "zod";

const optionalGuid = z.union([z.string().uuid("Must be a valid ID"), z.literal(""), z.undefined()]);

export const studentSchema = z.object({
  fullName: z.string().min(1, "Full name is required").max(150, "Maximum 150 characters"),
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters").optional(),
  phone: z.string().max(30, "Maximum 30 characters").optional().or(z.literal("")),
  address: z.string().max(250, "Maximum 250 characters").optional().or(z.literal("")),
  studentCode: z.string().min(1, "Student code is required").max(50, "Maximum 50 characters"),
  dateOfBirth: z.string().min(1, "Date of birth is required"),
  gender: z.enum(["Male", "Female", "Other"], {
    required_error: "Gender is required"
  }),
  admissionDate: z.string().min(1, "Admission date is required"),
  parentId: optionalGuid,
  classId: optionalGuid
});

export type StudentFormValues = z.infer<typeof studentSchema>;
