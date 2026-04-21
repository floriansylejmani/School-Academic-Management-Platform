export type FeeStatus = "Pending" | "Paid" | "Overdue" | "PartiallyPaid";
export type FeeStatusValue = 1 | 2 | 3 | 4;
export type PaymentMethod = "Cash" | "Card" | "BankTransfer" | "Online";
export type PaymentMethodValue = 1 | 2 | 3 | 4;

export interface Payment {
  id: string;
  feeId: string;
  feeType: string;
  studentId: string;
  studentName: string;
  amountPaid: number;
  paymentDate: string;
  paymentMethod: PaymentMethod | PaymentMethodValue;
  transactionReference?: string | null;
}

export interface Fee {
  id: string;
  studentId: string;
  studentName: string;
  feeType: string;
  amount: number;
  dueDate: string;
  status: FeeStatus;
  payments: Payment[];
  createdAt: string;
}

export interface FeeFilterParams {
  studentId?: string;
  status?: FeeStatus;
  dueDateFrom?: string;
  dueDateTo?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface PaymentFilterParams {
  studentId?: string;
  feeId?: string;
  dateFrom?: string;
  dateTo?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateFeeDto {
  studentId: string;
  feeType: string;
  amount: number;
  dueDate: string;
  status: FeeStatusValue;
}

export interface UpdateFeeDto {
  studentId: string;
  feeType: string;
  amount: number;
  dueDate: string;
  status: FeeStatusValue;
}

export interface CreatePaymentDto {
  feeId: string;
  amountPaid: number;
  paymentDate: string;
  paymentMethod: PaymentMethodValue;
  transactionReference?: string | null;
}
