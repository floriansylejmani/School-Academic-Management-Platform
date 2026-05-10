"use client";

import { FolderOpen } from "lucide-react";
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
import { StudentDocumentsPanel } from "@/features/files/components/student-documents-panel";
import { StudentForm } from "@/features/students/components/student-form";
import {
  useCreateStudent,
  useDeleteStudent,
  useStudents,
  useUpdateStudent
} from "@/features/students/hooks/use-students";
import type { Student, CreateStudentDto, UpdateStudentDto } from "@/features/students/types/student.types";

function getGenderLabel(gender: Student["gender"]) {
  switch (gender) {
    case 2:
      return "Female";
    case 3:
      return "Other";
    case 1:
    default:
      return "Male";
  }
}

export function StudentsAdminClient() {
  const studentsQuery = useStudents();
  const createStudent = useCreateStudent();
  const updateStudent = useUpdateStudent();
  const deleteStudent = useDeleteStudent();
  const [editingStudent, setEditingStudent] = useState<Student | null>(null);
  const [deletingStudent, setDeletingStudent] = useState<Student | null>(null);
  const [docsStudent, setDocsStudent] = useState<Student | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [search, setSearch] = useState("");

  const columns: DataTableColumn<Student>[] = [
    { key: "name", header: "Student", render: (student) => <div><p className="font-semibold">{student.fullName}</p><p className="text-slate-500">{student.email}</p></div> },
    { key: "studentCode", header: "Code", render: (student) => <Badge>{student.studentCode}</Badge> },
    { key: "class", header: "Class", render: (student) => student.className ?? "Unassigned" },
    { key: "gender", header: "Gender", render: (student) => getGenderLabel(student.gender) },
    { key: "admissionDate", header: "Admission", render: (student) => student.admissionDate },
    {
      key: "documents",
      header: "",
      render: (student) => (
        <button
          type="button"
          onClick={(e) => { e.stopPropagation(); setDocsStudent(student); }}
          className="flex items-center gap-1.5 rounded-lg px-2 py-1 text-xs text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-700"
          title="View documents"
        >
          <FolderOpen className="h-3.5 w-3.5" />
          Docs
        </button>
      )
    }
  ];

  const students = useMemo(() => studentsQuery.data?.items ?? [], [studentsQuery.data?.items]);
  const filteredStudents = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return students;

    return students.filter((student) => {
      const className = student.className ?? "";
      const gender = getGenderLabel(student.gender);
      return (
        [student.fullName, student.email, student.studentCode, className, gender]
          .filter(Boolean)
          .join(" ")
          .toLowerCase()
          .includes(q)
      );
    });
  }, [search, students]);

  if (studentsQuery.isLoading) {
    return <LoadingState title="Loading students..." description="Fetching the student directory." />;
  }

  if (studentsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load students"
        description="The student list could not be fetched right now. Check the backend connection and try again."
        action={<Button onClick={() => studentsQuery.refetch()}>Retry</Button>}
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Enrolment"
        title="Student Directory"
        description="Add, manage, and archive student profiles including admissions, class placement, and document records."
        actionLabel="Add student"
        onAction={() => setIsCreateOpen(true)}
      />

      {students.length > 0 ? (
        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search students by name, email, code, or class"
            className="w-72"
          />
          {search.trim() ? <Button variant="ghost" onClick={() => setSearch("")}>Clear</Button> : null}
        </div>
      ) : null}

      {filteredStudents.length === 0 ? (
        students.length === 0 ? (
          <EmptyState
            title="No students enrolled"
            description="Add the first student record to begin using attendance, results, and fee modules."
            action={<Button onClick={() => setIsCreateOpen(true)}>Add student</Button>}
          />
        ) : (
          <EmptyState
            title="No students match your search"
            description="Try searching by name, email, student code, class, or gender."
            action={<Button variant="secondary" onClick={() => setSearch("")}>Clear search</Button>}
          />
        )
      ) : (
        <DataTable
          columns={columns}
          rows={filteredStudents}
          getRowKey={(student) => student.id}
          onEdit={setEditingStudent}
          onDelete={setDeletingStudent}
        />
      )}

      <Modal
        open={isCreateOpen}
        title="Add student"
        description="Create a student account and academic profile."
        onClose={() => setIsCreateOpen(false)}
      >
        <StudentForm
          mode="create"
          isSubmitting={createStudent.isPending}
          onSubmit={async (payload) => {
            try {
              await createStudent.mutateAsync(payload as CreateStudentDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateStudent
            }
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingStudent)}
        title="Edit student"
        description="Update student identity and academic details."
        onClose={() => setEditingStudent(null)}
      >
        <StudentForm
          mode="edit"
          initialValues={editingStudent}
          isSubmitting={updateStudent.isPending}
          onSubmit={async (payload) => {
            if (!editingStudent) return;
            try {
              await updateStudent.mutateAsync({ id: editingStudent.id, payload: payload as UpdateStudentDto });
              setEditingStudent(null);
            } catch {
              // error handled via toast in useUpdateStudent
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingStudent)}
        title="Delete student"
        description={`This will permanently remove ${deletingStudent?.fullName ?? "this student"} from the system.`}
        onCancel={() => setDeletingStudent(null)}
        onConfirm={async () => {
          if (!deletingStudent) return;
          try {
            await deleteStudent.mutateAsync(deletingStudent.id);
            setDeletingStudent(null);
          } catch {
            // error handled via toast in useDeleteStudent
          }
        }}
        isPending={deleteStudent.isPending}
      />

      <Modal
        open={Boolean(docsStudent)}
        title={`Documents — ${docsStudent?.fullName ?? ""}`}
        description="Upload and manage documents for this student."
        onClose={() => setDocsStudent(null)}
      >
        {docsStudent && (
          <StudentDocumentsPanel studentId={docsStudent.id} canDelete />
        )}
      </Modal>
    </div>
  );
}
