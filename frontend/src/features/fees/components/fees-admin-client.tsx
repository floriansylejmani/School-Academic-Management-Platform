"use client";

import { useMemo, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDeleteDialog } from "@/components/ui/confirm-delete-dialog";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { FeeForm } from "@/features/fees/components/fee-form";
import { PaymentForm } from "@/features/fees/components/payment-form";
import { useAddPayment, useCreateFee, useDeleteFee, useFees, useUpdateFee } from "@/features/fees/hooks/use-fees";
import type { CreateFeeDto, Fee, FeeStatus, UpdateFeeDto } from "@/features/fees/types/fees.types";
import { useStudents } from "@/features/students/hooks/use-students";

const FEE_STATUS_OPTIONS: FeeStatus[] = ["Pending", "Paid", "Overdue", "PartiallyPaid"];

function getFeeStatusLabel(status: FeeStatus) {
  return status === "PartiallyPaid" ? "Partial" : status;
}

function getFeeStatusClassName(status: FeeStatus) {
  switch (status) {
    case "Paid":
      return "bg-emerald-50 text-emerald-700";
    case "Overdue":
      return "bg-rose-50 text-rose-700";
    case "PartiallyPaid":
      return "bg-blue-50 text-blue-700";
    case "Pending":
    default:
      return "bg-amber-50 text-amber-700";
  }
}

function getPaidAmount(fee: Fee) {
  return fee.payments.reduce((sum, payment) => sum + payment.amountPaid, 0);
}

const columns: DataTableColumn<Fee>[] = [
  {
    key: "student",
    header: "Student",
    render: (fee) => (
      <div>
        <p className="font-semibold text-slate-900">{fee.studentName}</p>
        <p className="text-slate-500">{fee.feeType}</p>
      </div>
    )
  },
  { key: "amount", header: "Amount", render: (fee) => `$${fee.amount.toFixed(2)}` },
  { key: "paid", header: "Paid", render: (fee) => `$${getPaidAmount(fee).toFixed(2)}` },
  { key: "dueDate", header: "Due date", render: (fee) => fee.dueDate },
  {
    key: "status",
    header: "Status",
    render: (fee) => <Badge className={getFeeStatusClassName(fee.status)}>{getFeeStatusLabel(fee.status)}</Badge>
  }
];

export function FeesAdminClient() {
  const studentsQuery = useStudents();
  const [filters, setFilters] = useState({
    studentId: "",
    status: "" as FeeStatus | "",
    dueDateFrom: "",
    dueDateTo: ""
  });
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [payingFee, setPayingFee] = useState<Fee | null>(null);
  const [editingFee, setEditingFee] = useState<Fee | null>(null);
  const [deletingFee, setDeletingFee] = useState<Fee | null>(null);

  const feeParams = useMemo(
    () => ({
      studentId: filters.studentId || undefined,
      status: filters.status || undefined,
      dueDateFrom: filters.dueDateFrom || undefined,
      dueDateTo: filters.dueDateTo || undefined
    }),
    [filters]
  );

  const feesQuery = useFees(feeParams);
  const createFee = useCreateFee();
  const updateFee = useUpdateFee();
  const deleteFee = useDeleteFee();
  const addPayment = useAddPayment();
  const students = studentsQuery.data?.items ?? [];
  const fees = feesQuery.data?.items ?? [];
  const columnsWithPayment = useMemo<DataTableColumn<Fee>[]>(
    () => [
      ...columns,
      {
        key: "payment",
        header: "Payment",
        render: (fee) => (
          <Button
            variant="secondary"
            className="h-9 px-3"
            disabled={fee.status === "Paid"}
            onClick={() => setPayingFee(fee)}
          >
            Record
          </Button>
        )
      }
    ],
    []
  );

  if (feesQuery.isLoading || studentsQuery.isLoading) {
    return <LoadingState title="Loading fees..." description="Fetching fee records and student roster." />;
  }

  if (feesQuery.isError || studentsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load fees"
        description="Fee data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void feesQuery.refetch();
              void studentsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  const hasFilters = Boolean(filters.studentId || filters.status || filters.dueDateFrom || filters.dueDateTo);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Fee Management"
        description="Create and manage student fee records. Track payment status and outstanding balances by student."
        actionLabel="Add fee"
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
          value={filters.status}
          onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value as FeeStatus | "" }))}
          placeholder="All statuses"
          className="w-44"
        >
          {FEE_STATUS_OPTIONS.map((status) => (
            <option key={status} value={status}>
              {getFeeStatusLabel(status)}
            </option>
          ))}
        </Select>
        <Input
          type="date"
          value={filters.dueDateFrom}
          onChange={(event) => setFilters((current) => ({ ...current, dueDateFrom: event.target.value }))}
          className="w-40"
        />
        <Input
          type="date"
          value={filters.dueDateTo}
          onChange={(event) => setFilters((current) => ({ ...current, dueDateTo: event.target.value }))}
          className="w-40"
        />
        {hasFilters ? (
          <Button
            variant="ghost"
            onClick={() => setFilters({ studentId: "", status: "", dueDateFrom: "", dueDateTo: "" })}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {fees.length === 0 ? (
        <EmptyState
          title="No fees found"
          description={hasFilters ? "No fee records match the selected filters." : "Create the first fee record to start tracking collections."}
          action={!hasFilters ? <Button onClick={() => setIsCreateOpen(true)}>Create fee</Button> : undefined}
        />
      ) : (
        <DataTable
          columns={columnsWithPayment}
          rows={fees}
          getRowKey={(fee) => fee.id}
          onEdit={setEditingFee}
          onDelete={setDeletingFee}
        />
      )}

      <Modal open={isCreateOpen} title="Create fee" description="Add a billable fee to a student account." onClose={() => setIsCreateOpen(false)}>
        <FeeForm
          mode="create"
          isSubmitting={createFee.isPending}
          onSubmit={async (payload) => {
            try {
              await createFee.mutateAsync(payload as CreateFeeDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateFee
            }
          }}
        />
      </Modal>

      <Modal open={Boolean(editingFee)} title="Edit fee" description="Update fee details and status." onClose={() => setEditingFee(null)}>
        <FeeForm
          mode="edit"
          initialValues={editingFee}
          isSubmitting={updateFee.isPending}
          onSubmit={async (payload) => {
            if (!editingFee) return;
            try {
              await updateFee.mutateAsync({ id: editingFee.id, payload: payload as UpdateFeeDto });
              setEditingFee(null);
            } catch {
              // error handled via toast in useUpdateFee
            }
          }}
        />
      </Modal>

      <Modal open={Boolean(payingFee)} title="Record payment" description="Apply a payment against a student fee." onClose={() => setPayingFee(null)}>
        <PaymentForm
          fees={fees}
          initialFeeId={payingFee?.id}
          isSubmitting={addPayment.isPending}
          onSubmit={async (payload) => {
            try {
              await addPayment.mutateAsync(payload);
              setPayingFee(null);
            } catch {
              // error handled via toast in useAddPayment
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingFee)}
        title="Delete fee"
        description={`This will permanently remove ${deletingFee?.feeType ?? "this fee"}. Fees with payments cannot be deleted.`}
        onCancel={() => setDeletingFee(null)}
        onConfirm={async () => {
          if (!deletingFee) return;
          try {
            await deleteFee.mutateAsync(deletingFee.id);
            setDeletingFee(null);
          } catch {
            // error handled via toast in useDeleteFee
          }
        }}
        isPending={deleteFee.isPending}
      />
    </div>
  );
}
