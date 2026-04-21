"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { paymentSchema, type PaymentFormValues } from "@/features/fees/schemas/fee.schema";
import type { CreatePaymentDto, Fee, PaymentMethod, PaymentMethodValue } from "@/features/fees/types/fees.types";

const PAYMENT_METHODS: PaymentMethod[] = ["Cash", "Card", "BankTransfer", "Online"];

function mapPaymentMethodToBackendValue(method: PaymentMethod): PaymentMethodValue {
  switch (method) {
    case "Card":
      return 2;
    case "BankTransfer":
      return 3;
    case "Online":
      return 4;
    case "Cash":
    default:
      return 1;
  }
}

function toNullable(value?: string) {
  return value && value.trim().length > 0 ? value : null;
}

function toLocalDateTimeInputValue(date: Date) {
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

interface PaymentFormProps {
  fees: Fee[];
  initialFeeId?: string;
  isSubmitting: boolean;
  onSubmit: (payload: CreatePaymentDto) => void;
}

export function PaymentForm({ fees, initialFeeId, isSubmitting, onSubmit }: PaymentFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<PaymentFormValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      feeId: initialFeeId ?? "",
      amountPaid: 0,
      paymentDate: toLocalDateTimeInputValue(new Date()),
      paymentMethod: "Cash",
      transactionReference: ""
    }
  });

  return (
    <form
      className="grid gap-5 md:grid-cols-2"
      onSubmit={handleSubmit((values) => {
        onSubmit({
          feeId: values.feeId,
          amountPaid: values.amountPaid,
          paymentDate: values.paymentDate,
          paymentMethod: mapPaymentMethodToBackendValue(values.paymentMethod),
          transactionReference: toNullable(values.transactionReference)
        });
      })}
    >
      <div className="md:col-span-2">
        <FormField label="Fee" error={errors.feeId?.message}>
          <Select {...register("feeId")} placeholder="Select fee">
            {fees.map((fee) => (
              <option key={fee.id} value={fee.id}>
                {fee.studentName} - {fee.feeType} - ${fee.amount.toFixed(2)}
              </option>
            ))}
          </Select>
        </FormField>
      </div>
      <FormField label="Amount paid" error={errors.amountPaid?.message}>
        <Input type="number" step="0.01" min="0" {...register("amountPaid")} />
      </FormField>
      <FormField label="Payment date" error={errors.paymentDate?.message}>
        <Input type="datetime-local" {...register("paymentDate")} />
      </FormField>
      <FormField label="Payment method" error={errors.paymentMethod?.message}>
        <Select {...register("paymentMethod")}>
          {PAYMENT_METHODS.map((method) => (
            <option key={method} value={method}>
              {method === "BankTransfer" ? "Bank transfer" : method}
            </option>
          ))}
        </Select>
      </FormField>
      <FormField label="Transaction reference" error={errors.transactionReference?.message}>
        <Input placeholder="Optional receipt or transfer reference" {...register("transactionReference")} />
      </FormField>
      <div className="md:col-span-2 flex justify-end">
        <Button type="submit" disabled={isSubmitting || fees.length === 0}>
          {isSubmitting ? "Recording..." : "Record payment"}
        </Button>
      </div>
    </form>
  );
}
