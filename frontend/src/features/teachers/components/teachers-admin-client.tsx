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
import { TeacherForm } from "@/features/teachers/components/teacher-form";
import {
  useCreateTeacher,
  useDeleteTeacher,
  useTeachers,
  useUpdateTeacher
} from "@/features/teachers/hooks/use-teachers";
import type { CreateTeacherDto, Teacher, UpdateTeacherDto } from "@/features/teachers/types/teacher.types";

const columns: DataTableColumn<Teacher>[] = [
  { key: "name", header: "Teacher", render: (teacher) => <div><p className="font-semibold">{teacher.fullName}</p><p className="text-slate-500">{teacher.email}</p></div> },
  { key: "teacherCode", header: "Code", render: (teacher) => <Badge>{teacher.teacherCode}</Badge> },
  { key: "specialization", header: "Specialization", render: (teacher) => teacher.specialization },
  { key: "hireDate", header: "Hire date", render: (teacher) => teacher.hireDate }
];

export function TeachersAdminClient() {
  const teachersQuery = useTeachers();
  const createTeacher = useCreateTeacher();
  const updateTeacher = useUpdateTeacher();
  const deleteTeacher = useDeleteTeacher();
  const [editingTeacher, setEditingTeacher] = useState<Teacher | null>(null);
  const [deletingTeacher, setDeletingTeacher] = useState<Teacher | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [search, setSearch] = useState("");

  if (teachersQuery.isLoading) {
    return <LoadingState title="Loading teachers..." description="Fetching the latest teaching staff records." />;
  }

  if (teachersQuery.isError) {
    return (
      <EmptyState
        title="Unable to load teachers"
        description="The teaching staff list could not be fetched right now."
        action={<Button onClick={() => teachersQuery.refetch()}>Retry</Button>}
      />
    );
  }

  const teachers = teachersQuery.data?.items ?? [];
  const filteredTeachers = teachers.filter((teacher) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;

    const classText = [teacher.fullName, teacher.email, teacher.teacherCode, teacher.specialization, teacher.hireDate]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();
    return classText.includes(q);
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Staff"
        title="Teaching Staff"
        description="Maintain staff profiles, specialisations, and codes used across the timetable, exams, and attendance."
        actionLabel="Add teacher"
        onAction={() => setIsCreateOpen(true)}
      />

      {teachers.length > 0 ? (
        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search teachers by name, email, code, or specialization"
            className="w-72"
          />
          {search.trim() ? <Button variant="ghost" onClick={() => setSearch("")}>Clear</Button> : null}
        </div>
      ) : null}

      {filteredTeachers.length === 0 ? (
        teachers.length === 0 ? (
          <EmptyState
            title="No staff records"
            description="Add the first teacher profile to begin timetable and exam assignments."
            action={<Button onClick={() => setIsCreateOpen(true)}>Add teacher</Button>}
          />
        ) : (
          <EmptyState
            title="No teachers match your search"
            description="Try searching by name, email, code, or specialization."
            action={<Button variant="secondary" onClick={() => setSearch("")}>Clear search</Button>}
          />
        )
      ) : (
        <DataTable
          columns={columns}
          rows={filteredTeachers}
          getRowKey={(teacher) => teacher.id}
          onEdit={setEditingTeacher}
          onDelete={setDeletingTeacher}
        />
      )}

      <Modal open={isCreateOpen} title="Add teacher" description="Create a staff profile and assign a subject specialisation." onClose={() => setIsCreateOpen(false)}>
        <TeacherForm
          mode="create"
          isSubmitting={createTeacher.isPending}
          onSubmit={async (payload) => {
            try {
              await createTeacher.mutateAsync(payload as CreateTeacherDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateTeacher
            }
          }}
        />
      </Modal>

      <Modal open={Boolean(editingTeacher)} title="Edit teacher" description="Update staff profile and contact details." onClose={() => setEditingTeacher(null)}>
        <TeacherForm
          mode="edit"
          initialValues={editingTeacher}
          isSubmitting={updateTeacher.isPending}
          onSubmit={async (payload) => {
            if (!editingTeacher) return;
            try {
              await updateTeacher.mutateAsync({ id: editingTeacher.id, payload: payload as UpdateTeacherDto });
              setEditingTeacher(null);
            } catch {
              // error handled via toast in useUpdateTeacher
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingTeacher)}
        title="Delete teacher"
        description={`This will permanently remove ${deletingTeacher?.fullName ?? "this teacher"} from the system.`}
        onCancel={() => setDeletingTeacher(null)}
        onConfirm={async () => {
          if (!deletingTeacher) return;
          try {
            await deleteTeacher.mutateAsync(deletingTeacher.id);
            setDeletingTeacher(null);
          } catch {
            // error handled via toast in useDeleteTeacher
          }
        }}
        isPending={deleteTeacher.isPending}
      />
    </div>
  );
}
