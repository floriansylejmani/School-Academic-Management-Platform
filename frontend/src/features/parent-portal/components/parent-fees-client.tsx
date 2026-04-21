"use client";

import { useMemo, useState } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useAuthStore } from "@/store/auth.store";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import { useChildFees } from "@/features/parent-portal/hooks/use-parent-portal";
import type { Fee, FeeStatus } from "@/features/fees/types/fees.types";

const STATUS_COLORS: Record<FeeStatus, string> = {
  Paid: "text-emerald-700 bg-emerald-50",
  Pending: "text-amber-700 bg-amber-50",
  Overdue: "text-rose-700 bg-rose-50",
  PartiallyPaid: "text-blue-700 bg-blue-50"
};

const ALL_STATUSES: FeeStatus[] = ["Pending", "Paid", "Overdue", "PartiallyPaid"];

function getFeeStatusLabel(status: FeeStatus) {
  return status === "PartiallyPaid" ? "Partial" : status;
}

const columns: DataTableColumn<Fee>[] = [
  {
    key: "title",
    header: "Fee",
    render: (f) => <span className="font-semibold text-slate-900">{f.feeType}</span>
  },
  {
    key: "amount",
    header: "Amount",
    render: (f) => (
      <span className="font-medium text-slate-700">${f.amount.toFixed(2)}</span>
    )
  },
  {
    key: "dueDate",
    header: "Due Date",
    render: (f) => f.dueDate
  },
  {
    key: "status",
    header: "Status",
    render: (f) => (
      <span
        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${STATUS_COLORS[f.status]}`}
      >
        {getFeeStatusLabel(f.status)}
      </span>
    )
  },
  {
    key: "paidAt",
    header: "Paid on",
    render: (f) => {
      const latestPayment = f.payments
        .slice()
        .sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime())[0];

      return latestPayment ? (
        <span className="text-slate-600">{latestPayment.paymentDate}</span>
      ) : (
        <span className="text-slate-400">-</span>
      );
    }
  }
];

export function ParentFeesClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];

  const [filterStatus, setFilterStatus] = useState<FeeStatus | "">("");
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);

  const feesQuery = useChildFees(activeChildId);

  const { fees, summary } = useMemo(() => {
    const all = feesQuery.data?.items ?? [];
    const filtered = filterStatus ? all.filter((f) => f.status === filterStatus) : all;
    const sorted = [...filtered].sort(
      (a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    );

    const totalAmount = all.reduce((sum, f) => sum + f.amount, 0);
    const paidAmount = all
      .filter((f) => f.status === "Paid")
      .reduce((sum, f) => sum + f.amount, 0);
    const pendingAmount = all
      .filter((f) => f.status !== "Paid")
      .reduce((sum, f) => sum + f.amount, 0);

    return { fees: sorted, summary: { totalAmount, paidAmount, pendingAmount } };
  }, [feesQuery.data, filterStatus]);

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading..." description="Fetching child profile." />;
  }

  if (childrenQuery.isError) {
    return (
      <EmptyState
        title="Unable to load children"
        description="Linked student profiles could not be loaded right now."
      />
    );
  }

  if (children.length === 0) {
    return (
      <EmptyState
        title="No child linked"
        description="No student is linked to your parent account."
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Fee Account"
        title="Fee Statement"
        description={`Fee records for ${activeChild?.fullName ?? "your child"} — paid, pending, and overdue balances.`}
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Total fees</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            ${summary.totalAmount.toFixed(2)}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Amount paid</p>
          <p className="mt-3 text-3xl font-semibold text-emerald-600">
            ${summary.paidAmount.toFixed(2)}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Outstanding</p>
          <p
            className={`mt-3 text-3xl font-semibold ${
              summary.pendingAmount > 0 ? "text-rose-600" : "text-emerald-600"
            }`}
          >
            ${summary.pendingAmount.toFixed(2)}
          </p>
        </Card>
      </div>

      <div className="flex flex-wrap gap-3">
        <ParentChildSwitcher students={children} value={activeChildId} onChange={setSelectedChildId} className="w-56" />

        <Select
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value as FeeStatus | "")}
          placeholder="All statuses"
          className="w-44"
        >
          {ALL_STATUSES.map((s) => (
            <option key={s} value={s}>
              {getFeeStatusLabel(s)}
            </option>
          ))}
        </Select>

        {filterStatus ? (
          <button
            type="button"
            onClick={() => setFilterStatus("")}
            className="text-sm text-brand-600 hover:underline"
          >
            Clear
          </button>
        ) : null}
      </div>

      {feesQuery.isLoading ? (
        <LoadingState title="Loading fees..." description="Fetching fee records." />
      ) : feesQuery.isError ? (
        <EmptyState
          title="Unable to load fees"
          description="Fee records could not be loaded for the selected child."
        />
      ) : fees.length === 0 ? (
        <EmptyState
          title="No fees found"
          description={
            filterStatus
              ? `No fees with status "${filterStatus}" found.`
              : "No fee records have been added yet."
          }
        />
      ) : (
        <DataTable
          columns={columns}
          rows={fees}
          getRowKey={(f) => f.id}
        />
      )}
    </div>
  );
}
