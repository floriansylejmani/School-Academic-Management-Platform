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
import { SubjectForm } from "@/features/subjects/components/subject-form";
import {
  useCreateSubject,
  useDeleteSubject,
  useSubjects,
  useUpdateSubject
} from "@/features/subjects/hooks/use-subjects";
import type { CreateSubjectDto, Subject, UpdateSubjectDto } from "@/features/subjects/types/subject.types";

const columns: DataTableColumn<Subject>[] = [
  { key: "name", header: "Subject", render: (subject) => <div><p className="font-semibold">{subject.name}</p><p className="text-slate-500">{subject.description ?? "No description provided"}</p></div> },
  { key: "code", header: "Code", render: (subject) => <Badge>{subject.code}</Badge> },
  { key: "createdAt", header: "Created", render: (subject) => subject.createdAt.slice(0, 10) }
];

export function SubjectsAdminClient() {
  const subjectsQuery = useSubjects();
  const createSubject = useCreateSubject();
  const updateSubject = useUpdateSubject();
  const deleteSubject = useDeleteSubject();
  const [editingSubject, setEditingSubject] = useState<Subject | null>(null);
  const [deletingSubject, setDeletingSubject] = useState<Subject | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [search, setSearch] = useState("");

  if (subjectsQuery.isLoading) {
    return <LoadingState title="Loading subjects..." description="Fetching the subject catalog from the backend." />;
  }

  if (subjectsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load subjects"
        description="The subject catalog could not be fetched right now."
        action={<Button onClick={() => subjectsQuery.refetch()}>Retry</Button>}
      />
    );
  }

  const subjects = subjectsQuery.data?.items ?? [];
  const filteredSubjects = subjects.filter((subject) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;

    const searchable = [
      subject.name,
      subject.code,
      subject.description ?? "",
      subject.createdAt?.slice(0, 10) ?? ""
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();

    return searchable.includes(q);
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Curriculum"
        title="Subject Catalogue"
        description="Manage subject names, codes, and descriptions used across the timetable, exams, and results."
        actionLabel="Add subject"
        onAction={() => setIsCreateOpen(true)}
      />

      {subjects.length > 0 ? (
        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search subjects by name, code, or description"
            className="w-72"
          />
          {search.trim() ? <Button variant="ghost" onClick={() => setSearch("")}>Clear</Button> : null}
        </div>
      ) : null}

      {filteredSubjects.length === 0 ? (
        subjects.length === 0 ? (
          <EmptyState
            title="No subjects found"
            description="Create the first subject to start timetables, exams, and academic assignment flows."
            action={<Button onClick={() => setIsCreateOpen(true)}>Create subject</Button>}
          />
        ) : (
          <EmptyState
            title="No subjects match your search"
            description="Try searching by subject name, code, or description."
            action={<Button variant="secondary" onClick={() => setSearch("")}>Clear search</Button>}
          />
        )
      ) : (
        <DataTable
          columns={columns}
          rows={filteredSubjects}
          getRowKey={(subject) => subject.id}
          onEdit={setEditingSubject}
          onDelete={setDeletingSubject}
        />
      )}

      <Modal open={isCreateOpen} title="Create subject" description="Add a new subject to the academic catalog." onClose={() => setIsCreateOpen(false)}>
        <SubjectForm
          mode="create"
          isSubmitting={createSubject.isPending}
          onSubmit={async (payload) => {
            try {
              await createSubject.mutateAsync(payload as CreateSubjectDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateSubject
            }
          }}
        />
      </Modal>

      <Modal open={Boolean(editingSubject)} title="Edit subject" description="Update subject details." onClose={() => setEditingSubject(null)}>
        <SubjectForm
          mode="edit"
          initialValues={editingSubject}
          isSubmitting={updateSubject.isPending}
          onSubmit={async (payload) => {
            if (!editingSubject) return;
            try {
              await updateSubject.mutateAsync({ id: editingSubject.id, payload: payload as UpdateSubjectDto });
              setEditingSubject(null);
            } catch {
              // error handled via toast in useUpdateSubject
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingSubject)}
        title="Delete subject"
        description={`This will permanently remove ${deletingSubject?.name ?? "this subject"} from the catalog.`}
        onCancel={() => setDeletingSubject(null)}
        onConfirm={async () => {
          if (!deletingSubject) return;
          try {
            await deleteSubject.mutateAsync(deletingSubject.id);
            setDeletingSubject(null);
          } catch {
            // error handled via toast in useDeleteSubject
          }
        }}
        isPending={deleteSubject.isPending}
      />
    </div>
  );
}
