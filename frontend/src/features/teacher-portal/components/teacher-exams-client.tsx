"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { ConfirmDeleteDialog } from "@/components/ui/confirm-delete-dialog";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { ExamForm } from "@/features/exams/components/exam-form";
import { useCreateExam, useDeleteExam, useExams, useUpdateExam } from "@/features/exams/hooks/use-exams";
import type { CreateExamDto, Exam, UpdateExamDto } from "@/features/exams/types/exams.types";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";

const columns: DataTableColumn<Exam>[] = [
  {
    key: "title",
    header: "Exam",
    render: (exam) => <span className="font-semibold text-slate-900">{exam.title}</span>
  },
  { key: "class", header: "Class", render: (exam) => exam.className },
  { key: "subject", header: "Subject", render: (exam) => exam.subjectName },
  { key: "date", header: "Date", render: (exam) => exam.examDate },
  {
    key: "marks",
    header: "Total marks",
    render: (exam) => <span className="font-medium text-slate-700">{exam.totalMarks}</span>
  }
];

export function TeacherExamsClient() {
  const examsQuery = useExams();
  const classesQuery = useClasses();
  const subjectsQuery = useSubjects();
  const createExam = useCreateExam();
  const updateExam = useUpdateExam();
  const deleteExam = useDeleteExam();

  const [editingExam, setEditingExam] = useState<Exam | null>(null);
  const [deletingExam, setDeletingExam] = useState<Exam | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [filterClassId, setFilterClassId] = useState("");
  const [filterSubjectId, setFilterSubjectId] = useState("");

  const classes = classesQuery.data?.items ?? [];
  const subjects = subjectsQuery.data?.items ?? [];

  const exams = useMemo(() => {
    const all = examsQuery.data?.items ?? [];
    return all.filter((exam) => {
      if (filterClassId && exam.classId !== filterClassId) return false;
      if (filterSubjectId && exam.subjectId !== filterSubjectId) return false;
      return true;
    });
  }, [examsQuery.data, filterClassId, filterSubjectId]);

  if (examsQuery.isLoading || classesQuery.isLoading || subjectsQuery.isLoading) {
    return (
      <LoadingState
        title="Loading teacher exams..."
        description="Preparing your class, subject, and exam schedule."
      />
    );
  }

  if (examsQuery.isError || classesQuery.isError || subjectsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load exams"
        description="Teacher exam data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void examsQuery.refetch();
              void classesQuery.refetch();
              void subjectsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  if (classes.length === 0 || subjects.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Assessments"
          title="Exam Schedule"
          description="Schedule and manage assessments for the classes and subjects assigned to your account."
        />
        <EmptyState
          title="No exam scope available"
          description="Your account does not have enough class and subject assignments to schedule exams yet."
        />
      </div>
    );
  }

  const hasFilters = Boolean(filterClassId || filterSubjectId);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Teacher / Exams"
        title="Exams"
        description="Schedule and manage assessments scoped to your assigned classes and subjects."
        actionLabel="Schedule exam"
        onAction={() => setIsCreateOpen(true)}
      />

      <div className="flex flex-wrap gap-3">
        <Select
          value={filterClassId}
          onChange={(event) => setFilterClassId(event.target.value)}
          placeholder="All classes"
          className="w-48"
        >
          {classes.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} {item.section}
            </option>
          ))}
        </Select>

        <Select
          value={filterSubjectId}
          onChange={(event) => setFilterSubjectId(event.target.value)}
          placeholder="All subjects"
          className="w-48"
        >
          {subjects.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </Select>

        {hasFilters ? (
          <Button
            variant="ghost"
            onClick={() => {
              setFilterClassId("");
              setFilterSubjectId("");
            }}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {exams.length === 0 ? (
        <EmptyState
          title="No exams found"
          description={
            hasFilters
              ? "No exams match the selected filters. Try adjusting or clearing them."
              : "Create the first exam for one of your assigned classes."
          }
          action={!hasFilters ? <Button onClick={() => setIsCreateOpen(true)}>Create exam</Button> : undefined}
        />
      ) : (
        <DataTable
          columns={columns}
          rows={exams}
          getRowKey={(exam) => exam.id}
          onEdit={setEditingExam}
          onDelete={setDeletingExam}
        />
      )}

      <Modal
        open={isCreateOpen}
        title="Create exam"
        description="Schedule a new exam for one of your assigned class and subject combinations."
        onClose={() => setIsCreateOpen(false)}
      >
        <ExamForm
          mode="create"
          classes={classes}
          subjects={subjects}
          isSubmitting={createExam.isPending}
          onSubmit={async (payload) => {
            await createExam.mutateAsync(payload as CreateExamDto);
            setIsCreateOpen(false);
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingExam)}
        title="Edit exam"
        description="Update the exam details inside your teacher scope."
        onClose={() => setEditingExam(null)}
      >
        <ExamForm
          mode="edit"
          initialValues={editingExam}
          classes={classes}
          subjects={subjects}
          isSubmitting={updateExam.isPending}
          onSubmit={async (payload) => {
            if (!editingExam) return;
            await updateExam.mutateAsync({ id: editingExam.id, payload: payload as UpdateExamDto });
            setEditingExam(null);
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingExam)}
        title="Delete exam"
        description={`This will permanently remove "${deletingExam?.title ?? "this exam"}" from your exam list.`}
        onCancel={() => setDeletingExam(null)}
        onConfirm={async () => {
          if (!deletingExam) return;
          await deleteExam.mutateAsync(deletingExam.id);
          setDeletingExam(null);
        }}
        isPending={deleteExam.isPending}
      />
    </div>
  );
}
