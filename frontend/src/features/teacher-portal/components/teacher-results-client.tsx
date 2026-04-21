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
import { useExams } from "@/features/exams/hooks/use-exams";
import { ResultForm } from "@/features/results/components/result-form";
import { useCreateResult, useDeleteResult, useResults, useUpdateResult } from "@/features/results/hooks/use-results";
import type { CreateResultDto, Result, UpdateResultDto } from "@/features/results/types/results.types";
import { useStudents } from "@/features/students/hooks/use-students";
import { useSubjects } from "@/features/subjects/hooks/use-subjects";

const GRADE_COLORS: Record<string, string> = {
  "A+": "text-emerald-700 bg-emerald-50",
  A: "text-emerald-600 bg-emerald-50",
  "B+": "text-blue-700 bg-blue-50",
  B: "text-blue-600 bg-blue-50",
  "C+": "text-amber-700 bg-amber-50",
  C: "text-amber-600 bg-amber-50",
  D: "text-orange-600 bg-orange-50",
  F: "text-rose-700 bg-rose-50"
};

const columns: DataTableColumn<Result>[] = [
  {
    key: "student",
    header: "Student",
    render: (result) => (
      <div>
        <p className="font-semibold text-slate-900">{result.studentName}</p>
        <p className="text-xs text-slate-500">{result.className}</p>
      </div>
    )
  },
  {
    key: "exam",
    header: "Exam",
    render: (result) => (
      <div>
        <p className="font-medium text-slate-700">{result.examTitle}</p>
        <p className="text-xs text-slate-500">{result.subjectName}</p>
      </div>
    )
  },
  {
    key: "marks",
    header: "Marks",
    render: (result) => (
      <span className="font-medium text-slate-700">
        {result.marksObtained} / {result.totalMarks}
      </span>
    )
  },
  {
    key: "grade",
    header: "Grade",
    render: (result) => (
      <span
        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${
          GRADE_COLORS[result.grade] ?? "text-slate-600 bg-slate-100"
        }`}
      >
        {result.grade}
      </span>
    )
  },
  {
    key: "remarks",
    header: "Remarks",
    render: (result) =>
      result.remarks ? <span className="text-slate-600">{result.remarks}</span> : <span className="text-slate-400">-</span>
  }
];

export function TeacherResultsClient() {
  const resultsQuery = useResults();
  const classesQuery = useClasses();
  const studentsQuery = useStudents();
  const examsQuery = useExams();
  const subjectsQuery = useSubjects();
  const createResult = useCreateResult();
  const updateResult = useUpdateResult();
  const deleteResult = useDeleteResult();

  const [editingResult, setEditingResult] = useState<Result | null>(null);
  const [deletingResult, setDeletingResult] = useState<Result | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [filterClassId, setFilterClassId] = useState("");
  const [filterStudentId, setFilterStudentId] = useState("");
  const [filterExamId, setFilterExamId] = useState("");
  const [filterSubjectId, setFilterSubjectId] = useState("");

  const classes = classesQuery.data?.items ?? [];
  const students = studentsQuery.data?.items ?? [];
  const exams = examsQuery.data?.items ?? [];
  const subjects = subjectsQuery.data?.items ?? [];

  const results = useMemo(() => {
    const all = resultsQuery.data?.items ?? [];
    return all.filter((result) => {
      if (filterClassId && result.classId !== filterClassId) return false;
      if (filterStudentId && result.studentId !== filterStudentId) return false;
      if (filterExamId && result.examId !== filterExamId) return false;
      if (filterSubjectId && result.subjectId !== filterSubjectId) return false;
      return true;
    });
  }, [filterClassId, filterExamId, filterStudentId, filterSubjectId, resultsQuery.data]);

  const hasFilters = Boolean(filterClassId || filterStudentId || filterExamId || filterSubjectId);

  if (
    resultsQuery.isLoading ||
    classesQuery.isLoading ||
    studentsQuery.isLoading ||
    examsQuery.isLoading ||
    subjectsQuery.isLoading
  ) {
    return (
      <LoadingState
        title="Loading teacher results..."
        description="Preparing your students, exams, subjects, and recorded results."
      />
    );
  }

  if (
    resultsQuery.isError ||
    classesQuery.isError ||
    studentsQuery.isError ||
    examsQuery.isError ||
    subjectsQuery.isError
  ) {
    return (
      <EmptyState
        title="Unable to load results"
        description="Teacher result data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void resultsQuery.refetch();
              void classesQuery.refetch();
              void studentsQuery.refetch();
              void examsQuery.refetch();
              void subjectsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  if (exams.length === 0 || students.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Academic Records"
          title="Assessment Results"
          description="Record marks and grades for the exams and students within your assigned scope."
        />
        <EmptyState
          title="No result scope available"
          description="You need assigned exams and students before results can be recorded."
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Teacher / Results"
        title="Results"
        description="Record and maintain grades for students and exams scoped to your teacher account."
        actionLabel="Record result"
        onAction={() => setIsCreateOpen(true)}
      />

      <div className="flex flex-wrap gap-3">
        <Select value={filterClassId} onChange={(event) => setFilterClassId(event.target.value)} placeholder="All classes" className="w-44">
          {classes.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} {item.section}
            </option>
          ))}
        </Select>

        <Select value={filterStudentId} onChange={(event) => setFilterStudentId(event.target.value)} placeholder="All students" className="w-48">
          {students.map((student) => (
            <option key={student.id} value={student.id}>
              {student.fullName}
            </option>
          ))}
        </Select>

        <Select value={filterExamId} onChange={(event) => setFilterExamId(event.target.value)} placeholder="All exams" className="w-48">
          {exams.map((exam) => (
            <option key={exam.id} value={exam.id}>
              {exam.title}
            </option>
          ))}
        </Select>

        <Select value={filterSubjectId} onChange={(event) => setFilterSubjectId(event.target.value)} placeholder="All subjects" className="w-44">
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
              setFilterStudentId("");
              setFilterExamId("");
              setFilterSubjectId("");
            }}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {results.length === 0 ? (
        <EmptyState
          title="No results found"
          description={
            hasFilters
              ? "No results match the selected filters. Try adjusting or clearing them."
              : "Record the first result for one of your scheduled exams."
          }
          action={!hasFilters ? <Button onClick={() => setIsCreateOpen(true)}>Record result</Button> : undefined}
        />
      ) : (
        <DataTable
          columns={columns}
          rows={results}
          getRowKey={(result) => result.id}
          onEdit={setEditingResult}
          onDelete={setDeletingResult}
        />
      )}

      <Modal
        open={isCreateOpen}
        title="Record result"
        description="Assign marks and a grade for a student inside your current teaching scope."
        onClose={() => setIsCreateOpen(false)}
      >
        <ResultForm
          mode="create"
          exams={exams}
          students={students}
          isSubmitting={createResult.isPending}
          onSubmit={async (payload) => {
            await createResult.mutateAsync(payload as CreateResultDto);
            setIsCreateOpen(false);
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingResult)}
        title="Edit result"
        description="Update marks, grade, or remarks for this result."
        onClose={() => setEditingResult(null)}
      >
        <ResultForm
          mode="edit"
          initialValues={editingResult}
          exams={exams}
          students={students}
          isSubmitting={updateResult.isPending}
          onSubmit={async (payload) => {
            if (!editingResult) return;
            await updateResult.mutateAsync({ id: editingResult.id, payload: payload as UpdateResultDto });
            setEditingResult(null);
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingResult)}
        title="Delete result"
        description={`This will permanently remove the result for ${deletingResult?.studentName ?? "this student"} in "${deletingResult?.examTitle ?? "this exam"}".`}
        onCancel={() => setDeletingResult(null)}
        onConfirm={async () => {
          if (!deletingResult) return;
          await deleteResult.mutateAsync(deletingResult.id);
          setDeletingResult(null);
        }}
        isPending={deleteResult.isPending}
      />
    </div>
  );
}
