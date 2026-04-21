"use client";

import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDeleteDialog } from "@/components/ui/confirm-delete-dialog";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { ParentForm } from "@/features/parents/components/parent-form";
import {
  useCreateParent,
  useDeleteParent,
  useParents,
  useUpdateParent
} from "@/features/parents/hooks/use-parents";
import type { CreateParentDto, Parent, UpdateParentDto } from "@/features/parents/types/parents.types";

const columns: DataTableColumn<Parent>[] = [
  { key: "name", header: "Parent", render: (parent) => <div><p className="font-semibold">{parent.fullName}</p><p className="text-slate-500">{parent.email}</p></div> },
  { key: "phone", header: "Phone", render: (parent) => parent.phone ?? "Not provided" },
  { key: "occupation", header: "Occupation", render: (parent) => parent.occupation ?? "Not provided" },
  { key: "students", header: "Children", render: (parent) => <Badge>{parent.studentsCount}</Badge> },
  { key: "address", header: "Address", render: (parent) => parent.address ?? "Not provided" }
];

export function ParentsAdminClient() {
  const parentsQuery = useParents();
  const createParent = useCreateParent();
  const updateParent = useUpdateParent();
  const deleteParent = useDeleteParent();
  const [editingParent, setEditingParent] = useState<Parent | null>(null);
  const [deletingParent, setDeletingParent] = useState<Parent | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [search, setSearch] = useState("");

  if (parentsQuery.isLoading) {
    return <LoadingState title="Loading parents..." description="Fetching the latest parent directory records." />;
  }

  if (parentsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load parents"
        description="The parent directory could not be fetched right now. Check the backend connection and try again."
        action={<Button onClick={() => parentsQuery.refetch()}>Retry</Button>}
      />
    );
  }

  const parents = parentsQuery.data?.items ?? [];
  const filteredParents = parents.filter((parent) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;

    const searchable = [
      parent.fullName,
      parent.email,
      parent.phone ?? "",
      parent.occupation ?? "",
      parent.address ?? "",
      String(parent.studentsCount ?? "")
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();

    return searchable.includes(q);
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Guardians"
        title="Parent & Guardian Directory"
        description="Manage guardian accounts, contact details, and their linked student relationships."
        actionLabel="Add parent"
        onAction={() => setIsCreateOpen(true)}
      />

      {parents.length > 0 ? (
        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search parents by name, email, phone, or address"
            className="w-72"
          />
          {search.trim() ? <Button variant="ghost" onClick={() => setSearch("")}>Clear</Button> : null}
        </div>
      ) : null}

      {filteredParents.length === 0 ? (
        parents.length === 0 ? (
          <EmptyState
            title="No guardians on record"
            description="Add the first guardian account to enable student linking and portal access."
            action={<Button onClick={() => setIsCreateOpen(true)}>Add parent</Button>}
          />
        ) : (
          <EmptyState
            title="No parents match your search"
            description="Try searching by name, email, phone, occupation, or address."
            action={<Button variant="secondary" onClick={() => setSearch("")}>Clear search</Button>}
          />
        )
      ) : (
        <DataTable
          columns={columns}
          rows={filteredParents}
          getRowKey={(parent) => parent.id}
          onEdit={setEditingParent}
          onDelete={setDeletingParent}
        />
      )}

      <Modal
        open={isCreateOpen}
        title="Create parent"
        description="Add a parent account and contact profile."
        onClose={() => setIsCreateOpen(false)}
      >
        <ParentForm
          mode="create"
          isSubmitting={createParent.isPending}
          onSubmit={async (payload) => {
            try {
              await createParent.mutateAsync(payload as CreateParentDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateParent
            }
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingParent)}
        title="Edit parent"
        description="Update parent identity and contact details."
        onClose={() => setEditingParent(null)}
      >
        <ParentForm
          mode="edit"
          initialValues={editingParent}
          isSubmitting={updateParent.isPending}
          onSubmit={async (payload) => {
            if (!editingParent) return;
            try {
              await updateParent.mutateAsync({ id: editingParent.id, payload: payload as UpdateParentDto });
              setEditingParent(null);
            } catch {
              // error handled via toast in useUpdateParent
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingParent)}
        title="Delete parent"
        description={`This will permanently remove ${deletingParent?.fullName ?? "this parent"} from the system. Linked students will be unassigned from this parent.`}
        onCancel={() => setDeletingParent(null)}
        onConfirm={async () => {
          if (!deletingParent) return;
          try {
            await deleteParent.mutateAsync(deletingParent.id);
            setDeletingParent(null);
          } catch {
            // error handled via toast in useDeleteParent
          }
        }}
        isPending={deleteParent.isPending}
      />
    </div>
  );
}
