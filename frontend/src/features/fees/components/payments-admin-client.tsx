"use client";

import { useMemo, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { PaymentForm } from "@/features/fees/components/payment-form";
import { useAddPayment, useFees, usePayments } from "@/features/fees/hooks/use-fees";
import type { Payment } from "@/features/fees/types/fees.types";
import { useStudents } from "@/features/students/hooks/use-students";

function getPaymentMethodLabel(method: Payment["paymentMethod"]) {
  if (typeof method === "string") {
    return method === "BankTransfer" ? "Bank transfer" : method;
  }

  switch (method) {
    case 2:
      return "Card";
    case 3:
      return "Bank transfer";
    case 4:
      return "Online";
    case 1:
    default:
      return "Cash";
  }
}

const columns: DataTableColumn<Payment>[] = [
  {
    key: "student",
    header: "Student",
    render: (payment) => (
      <div>
        <p className="font-semibold text-slate-900">{payment.studentName}</p>
        <p className="text-slate-500">{payment.feeType}</p>
      </div>
    )
  },
  { key: "amount", header: "Amount", render: (payment) => `$${payment.amountPaid.toFixed(2)}` },
  { key: "date", header: "Payment date", render: (payment) => new Date(payment.paymentDate).toLocaleString() },
  { key: "method", header: "Method", render: (payment) => <Badge>{getPaymentMethodLabel(payment.paymentMethod)}</Badge> },
  { key: "reference", header: "Reference", render: (payment) => payment.transactionReference ?? "Not provided" }
];

export function PaymentsAdminClient() {
  const studentsQuery = useStudents();
  const feesQuery = useFees();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [filters, setFilters] = useState({
    studentId: "",
    feeId: "",
    dateFrom: "",
    dateTo: ""
  });

  const paymentParams = useMemo(
    () => ({
      studentId: filters.studentId || undefined,
      feeId: filters.feeId || undefined,
      dateFrom: filters.dateFrom ? `${filters.dateFrom}T00:00:00` : undefined,
      dateTo: filters.dateTo ? `${filters.dateTo}T23:59:59` : undefined
    }),
    [filters]
  );

  const paymentsQuery = usePayments(paymentParams);
  const addPayment = useAddPayment();
  const students = studentsQuery.data?.items ?? [];
  const fees = feesQuery.data?.items ?? [];
  const payments = paymentsQuery.data?.items ?? [];
  const hasFilters = Boolean(filters.studentId || filters.feeId || filters.dateFrom || filters.dateTo);

  if (studentsQuery.isLoading || feesQuery.isLoading || paymentsQuery.isLoading) {
    return <LoadingState title="Loading payments..." description="Fetching payment history and open fees." />;
  }

  if (studentsQuery.isError || feesQuery.isError || paymentsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load payments"
        description="Payment data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void studentsQuery.refetch();
              void feesQuery.refetch();
              void paymentsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Payment Ledger"
        description="Record full or partial payments and review the complete payment history by student, fee type, and date."
        actionLabel="Add payment"
        onAction={() => setIsCreateOpen(true)}
      />

      <div className="flex flex-wrap gap-3">
        <Select
          value={filters.studentId}
          onChange={(event) => setFilters((current) => ({ ...current, studentId: event.target.value }))}
          placeholder="All students"
          className="w-56"
        >
          {students.map((student) => (
            <option key={student.id} value={student.id}>
              {student.fullName}
            </option>
          ))}
        </Select>
        <Select
          value={filters.feeId}
          onChange={(event) => setFilters((current) => ({ ...current, feeId: event.target.value }))}
          placeholder="All fees"
          className="w-64"
        >
          {fees.map((fee) => (
            <option key={fee.id} value={fee.id}>
              {fee.studentName} - {fee.feeType}
            </option>
          ))}
        </Select>
        <Input
          type="date"
          value={filters.dateFrom}
          onChange={(event) => setFilters((current) => ({ ...current, dateFrom: event.target.value }))}
          className="w-40"
        />
        <Input
          type="date"
          value={filters.dateTo}
          onChange={(event) => setFilters((current) => ({ ...current, dateTo: event.target.value }))}
          className="w-40"
        />
        {hasFilters ? (
          <Button variant="ghost" onClick={() => setFilters({ studentId: "", feeId: "", dateFrom: "", dateTo: "" })}>
            Clear filters
          </Button>
        ) : null}
      </div>

      {payments.length === 0 ? (
        <EmptyState
          title="No payments found"
          description={hasFilters ? "No payments match the selected filters." : "Add the first payment to start building finance history."}
          action={!hasFilters ? <Button onClick={() => setIsCreateOpen(true)}>Add payment</Button> : undefined}
        />
      ) : (
        <DataTable columns={columns} rows={payments} getRowKey={(payment) => payment.id} />
      )}

      <Modal open={isCreateOpen} title="Add payment" description="Apply a payment against an existing fee." onClose={() => setIsCreateOpen(false)}>
        <PaymentForm
          fees={fees}
          isSubmitting={addPayment.isPending}
          onSubmit={async (payload) => {
            try {
              await addPayment.mutateAsync(payload);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useAddPayment
            }
          }}
        />
      </Modal>
    </div>
  );
}
