"use client";

import { useState, useMemo } from "react";
import { Button } from "@/components/ui/button";
import { ConfirmDeleteDialog } from "@/components/ui/confirm-delete-dialog";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useClasses } from "@/features/classes/hooks/use-classes";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";
import { ExamForm } from "@/features/exams/components/exam-form";
import { useExams, useCreateExam, useUpdateExam, useDeleteExam } from "@/features/exams/hooks/use-exams";
import type { Exam, CreateExamDto, UpdateExamDto } from "@/features/exams/types/exams.types";

const columns: DataTableColumn<Exam>[] = [
  {
    key: "title",
    header: "Exam",
    render: (exam) => (
      <div>
        <p className="font-semibold text-slate-900">{exam.title}</p>
        <p className="text-slate-500">{exam.subjectName}</p>
      </div>
    )
  },
  {
    key: "class",
    header: "Class",
    render: (exam) => exam.className
  },
  {
    key: "date",
    header: "Date",
    render: (exam) => exam.examDate
  },
  {
    key: "marks",
    header: "Total marks",
    render: (exam) => (
      <span className="font-medium text-slate-700">{exam.totalMarks}</span>
    )
  }
];

export function ExamsAdminClient() {
  const examsQuery = useExams();
  const { data: classesData } = useClasses();
  const { data: subjectsData } = useSubjects();
  const createExam = useCreateExam();
  const updateExam = useUpdateExam();
  const deleteExam = useDeleteExam();

  const [editingExam, setEditingExam] = useState<Exam | null>(null);
  const [deletingExam, setDeletingExam] = useState<Exam | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [filterClassId, setFilterClassId] = useState("");
  const [filterSubjectId, setFilterSubjectId] = useState("");

  const classes = classesData?.items ?? [];
  const subjects = subjectsData?.items ?? [];

  const exams = useMemo(() => {
    const all = examsQuery.data?.items ?? [];
    return all.filter((exam) => {
      if (filterClassId && exam.classId !== filterClassId) return false;
      if (filterSubjectId && exam.subjectId !== filterSubjectId) return false;
      return true;
    });
  }, [examsQuery.data, filterClassId, filterSubjectId]);

  if (examsQuery.isLoading) {
    return (
      <LoadingState
        title="Loading exams..."
        description="Fetching the exam schedule from the API."
      />
    );
  }

  if (examsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load exams"
        description="The exam list could not be fetched right now. Check the backend connection and try again."
        action={<Button onClick={() => examsQuery.refetch()}>Retry</Button>}
      />
    );
  }

  const hasFilters = Boolean(filterClassId || filterSubjectId);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Assessments"
        title="Exam Schedule"
        description="Schedule assessments, assign them to classes and subjects, and track marks across the academic calendar."
        actionLabel="Schedule exam"
        onAction={() => setIsCreateOpen(true)}
      />

      <div className="flex flex-wrap items-center gap-3">
        <Select
          value={filterClassId}
          onChange={(e) => setFilterClassId(e.target.value)}
          placeholder="All classes"
          className="w-56"
        >
          {classes.map((cls) => (
            <option key={cls.id} value={cls.id}>
              {cls.name} {cls.section}
            </option>
          ))}
        </Select>

        <Select
          value={filterSubjectId}
          onChange={(e) => setFilterSubjectId(e.target.value)}
          placeholder="All subjects"
          className="w-56"
        >
          {subjects.map((subject) => (
            <option key={subject.id} value={subject.id}>
              {subject.name}
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
              : "Create the first exam to start tracking results and student performance."
          }
          action={
            !hasFilters ? (
              <Button onClick={() => setIsCreateOpen(true)}>Create exam</Button>
            ) : undefined
          }
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
        description="Schedule a new exam for a class and subject."
        onClose={() => setIsCreateOpen(false)}
      >
        <ExamForm
          mode="create"
          isSubmitting={createExam.isPending}
          onSubmit={async (payload) => {
            try {
              await createExam.mutateAsync(payload as CreateExamDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateExam
            }
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingExam)}
        title="Edit exam"
        description="Update the exam title, date, class, or total marks."
        onClose={() => setEditingExam(null)}
      >
        <ExamForm
          mode="edit"
          initialValues={editingExam}
          isSubmitting={updateExam.isPending}
          onSubmit={async (payload) => {
            if (!editingExam) return;
            try {
              await updateExam.mutateAsync({ id: editingExam.id, payload: payload as UpdateExamDto });
              setEditingExam(null);
            } catch {
              // error handled via toast in useUpdateExam
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingExam)}
        title="Delete exam"
        description={`This will permanently remove "${deletingExam?.title ?? "this exam"}" and all associated results.`}
        onCancel={() => setDeletingExam(null)}
        onConfirm={async () => {
          if (!deletingExam) return;
          try {
            await deleteExam.mutateAsync(deletingExam.id);
            setDeletingExam(null);
          } catch {
            // error handled via toast in useDeleteExam
          }
        }}
        isPending={deleteExam.isPending}
      />
    </div>
  );
}
