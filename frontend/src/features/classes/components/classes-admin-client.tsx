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
import { ClassForm } from "@/features/classes/components/class-form";
import { useClasses, useCreateClass, useDeleteClass, useUpdateClass } from "@/features/classes/hooks/use-classes";
import type { AcademicClass, CreateAcademicClassDto, UpdateAcademicClassDto } from "@/features/classes/types/class.types";

const columns: DataTableColumn<AcademicClass>[] = [
  { key: "name", header: "Class", render: (item) => <div><p className="font-semibold">{item.name}</p><p className="text-slate-500">Section {item.section}</p></div> },
  { key: "year", header: "Academic year", render: (item) => <Badge>{item.academicYear}</Badge> },
  { key: "teacher", header: "Class teacher", render: (item) => item.classTeacherName ?? "Not assigned" },
  { key: "students", header: "Students", render: (item) => item.studentCount }
];

export function ClassesAdminClient() {
  const classesQuery = useClasses();
  const createClass = useCreateClass();
  const updateClass = useUpdateClass();
  const deleteClass = useDeleteClass();
  const [editingClass, setEditingClass] = useState<AcademicClass | null>(null);
  const [deletingClass, setDeletingClass] = useState<AcademicClass | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [search, setSearch] = useState("");

  if (classesQuery.isLoading) {
    return <LoadingState title="Loading classes..." description="Fetching the current class structure and teacher assignments." />;
  }

  if (classesQuery.isError) {
    return (
      <EmptyState
        title="Unable to load classes"
        description="The class list could not be fetched right now."
        action={<Button onClick={() => classesQuery.refetch()}>Retry</Button>}
      />
    );
  }

  const classes = classesQuery.data?.items ?? [];
  const filteredClasses = classes.filter((cls) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;

    const searchable = [
      cls.name,
      cls.section,
      cls.academicYear,
      cls.classTeacherName ?? "",
      String(cls.studentCount ?? "")
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();

    return searchable.includes(q);
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academic Groups"
        title="Classes"
        description="Configure class groups, year levels, sections, and homeroom teacher assignments."
        actionLabel="Create class"
        onAction={() => setIsCreateOpen(true)}
      />

      {classes.length > 0 ? (
        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search classes by name, section, year, teacher"
            className="w-72"
          />
          {search.trim() ? <Button variant="ghost" onClick={() => setSearch("")}>Clear</Button> : null}
        </div>
      ) : null}

      {filteredClasses.length === 0 ? (
        classes.length === 0 ? (
          <EmptyState
            title="No classes configured"
            description="Add the first class group to enable student placement and timetable scheduling."
            action={<Button onClick={() => setIsCreateOpen(true)}>Create class</Button>}
          />
        ) : (
          <EmptyState
            title="No classes match your search"
            description="Try searching by class name, section, academic year, or teacher."
            action={<Button variant="secondary" onClick={() => setSearch("")}>Clear search</Button>}
          />
        )
      ) : (
        <DataTable
          columns={columns}
          rows={filteredClasses}
          getRowKey={(item) => item.id}
          onEdit={setEditingClass}
          onDelete={setDeletingClass}
        />
      )}

      <Modal open={isCreateOpen} title="Create class" description="Add a new academic class." onClose={() => setIsCreateOpen(false)}>
        <ClassForm
          mode="create"
          isSubmitting={createClass.isPending}
          onSubmit={async (payload) => {
            try {
              await createClass.mutateAsync(payload as CreateAcademicClassDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateClass
            }
          }}
        />
      </Modal>

      <Modal open={Boolean(editingClass)} title="Edit class" description="Update class details." onClose={() => setEditingClass(null)}>
        <ClassForm
          mode="edit"
          initialValues={editingClass}
          isSubmitting={updateClass.isPending}
          onSubmit={async (payload) => {
            if (!editingClass) return;
            try {
              await updateClass.mutateAsync({ id: editingClass.id, payload: payload as UpdateAcademicClassDto });
              setEditingClass(null);
            } catch {
              // error handled via toast in useUpdateClass
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingClass)}
        title="Delete class"
        description={`This will permanently remove ${deletingClass?.name ?? "this class"} ${deletingClass?.section ?? ""}.`}
        onCancel={() => setDeletingClass(null)}
        onConfirm={async () => {
          if (!deletingClass) return;
          try {
            await deleteClass.mutateAsync(deletingClass.id);
            setDeletingClass(null);
          } catch {
            // error handled via toast in useDeleteClass
          }
        }}
        isPending={deleteClass.isPending}
      />
    </div>
  );
}
