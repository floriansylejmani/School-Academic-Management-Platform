import { z } from "zod";

export const feeSchema = z.object({
  studentId: z.string().min(1, "Student is required"),
  feeType: z.string().min(1, "Fee type is required").max(100, "Maximum 100 characters"),
  amount: z.coerce.number().min(0, "Amount must be 0 or greater"),
  dueDate: z.string().min(1, "Due date is required"),
  status: z.enum(["Pending", "Paid", "Overdue", "PartiallyPaid"], {
    required_error: "Status is required"
  })
});

export const paymentSchema = z.object({
  feeId: z.string().min(1, "Fee is required"),
  amountPaid: z.coerce.number().positive("Amount paid must be greater than 0"),
  paymentDate: z.string().min(1, "Payment date is required"),
  paymentMethod: z.enum(["Cash", "Card", "BankTransfer", "Online"], {
    required_error: "Payment method is required"
  }),
  transactionReference: z.string().max(100, "Maximum 100 characters").optional().or(z.literal(""))
});

export type FeeFormValues = z.infer<typeof feeSchema>;
export type PaymentFormValues = z.infer<typeof paymentSchema>;
