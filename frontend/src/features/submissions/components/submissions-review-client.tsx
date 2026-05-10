"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { BookCopy, Bot, Sparkles } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { SubmissionAIInsights } from "@/features/submissions/components/submission-ai-insights";
import {
  requestSubmissionAISchema,
  teacherReviewSchema,
  type RequestSubmissionAIFormValues,
  type TeacherReviewFormValues
} from "@/features/submissions/schemas/submission.schema";
import {
  useRequestSubmissionAiFeedback,
  useRequestSubmissionSmartGrade,
  useSubmissions,
  useUpdateSubmissionTeacherReview
} from "@/features/submissions/hooks/use-submissions";
import type { Submission } from "@/features/submissions/types/submissions.types";

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function formatScore(value: number | null | undefined) {
  return typeof value === "number" ? value.toString() : "Pending";
}

function createColumns(): DataTableColumn<Submission>[] {
  return [
    {
      key: "student",
      header: "Student",
      render: (submission) => (
        <div>
          <p className="font-semibold text-slate-900">{submission.studentName}</p>
          <p className="text-xs text-slate-500">{submission.className}</p>
        </div>
      )
    },
    {
      key: "exam",
      header: "Assessment",
      render: (submission) => (
        <div>
          <p className="font-medium text-slate-700">{submission.examTitle}</p>
          <p className="text-xs text-slate-500">{submission.subjectName}</p>
        </div>
      )
    },
    {
      key: "ai",
      header: "AI Status",
      render: (submission) => (
        <span
          className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ${
            submission.hasAIReview ? "bg-brand-50 text-brand-700" : "bg-slate-100 text-slate-600"
          }`}
        >
          {submission.hasAIReview ? submission.aiReview?.mode === "SmartGrade" ? "Smart grade ready" : "Feedback ready" : "Not generated"}
        </span>
      )
    },
    {
      key: "teacher",
      header: "Teacher Score",
      render: (submission) => (
        <div>
          <p className="font-medium text-slate-700">
            {formatScore(submission.teacherFinalScore)} / {submission.maximumScore}
          </p>
          <p className="text-xs text-slate-500">{submission.teacherFinalGrade ?? "Grade pending"}</p>
        </div>
      )
    },
    {
      key: "submitted",
      header: "Submitted",
      render: (submission) => <span className="text-sm text-slate-600">{formatDateTime(submission.submittedAt)}</span>
    }
  ];
}

export function SubmissionsReviewClient() {
  const submissionsQuery = useSubmissions({ pageNumber: 1, pageSize: 100 });
  const requestAiFeedback = useRequestSubmissionAiFeedback();
  const requestSmartGrade = useRequestSubmissionSmartGrade();
  const updateTeacherReview = useUpdateSubmissionTeacherReview();

  const [selectedSubmission, setSelectedSubmission] = useState<Submission | null>(null);
  const [filterExamId, setFilterExamId] = useState("");
  const [filterStudentId, setFilterStudentId] = useState("");

  const aiForm = useForm<RequestSubmissionAIFormValues>({
    resolver: zodResolver(requestSubmissionAISchema),
    defaultValues: {
      rubricInstructions: null,
      additionalInstructions: null
    }
  });

  const reviewForm = useForm<TeacherReviewFormValues>({
    resolver: zodResolver(teacherReviewSchema),
    defaultValues: {
      teacherFinalScore: null,
      teacherFinalGrade: null,
      teacherReviewNotes: null,
      isAiFeedbackReleasedToStudent: false
    }
  });

  useEffect(() => {
    aiForm.reset({
      rubricInstructions: null,
      additionalInstructions: null
    });

    reviewForm.reset({
      teacherFinalScore: selectedSubmission?.teacherFinalScore ?? null,
      teacherFinalGrade: selectedSubmission?.teacherFinalGrade ?? null,
      teacherReviewNotes: selectedSubmission?.teacherReviewNotes ?? null,
      isAiFeedbackReleasedToStudent: selectedSubmission?.isAiFeedbackReleasedToStudent ?? false
    });
  }, [aiForm, reviewForm, selectedSubmission]);

  const submissions = useMemo(() => submissionsQuery.data?.items ?? [], [submissionsQuery.data?.items]);
  const examOptions = useMemo(
    () =>
      Array.from(new Map(submissions.map((submission) => [submission.examId, submission])).values()).sort((left, right) =>
        left.examTitle.localeCompare(right.examTitle)),
    [submissions]
  );
  const studentOptions = useMemo(
    () =>
      Array.from(new Map(submissions.map((submission) => [submission.studentId, submission])).values()).sort((left, right) =>
        left.studentName.localeCompare(right.studentName)),
    [submissions]
  );
  const filteredSubmissions = useMemo(
    () =>
      submissions.filter((submission) => {
        if (filterExamId && submission.examId !== filterExamId) {
          return false;
        }

        if (filterStudentId && submission.studentId !== filterStudentId) {
          return false;
        }

        return true;
      }),
    [filterExamId, filterStudentId, submissions]
  );

  if (submissionsQuery.isLoading) {
    return (
      <LoadingState
        title="Loading submissions..."
        description="Fetching student essay submissions and AI review status."
      />
    );
  }

  if (submissionsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load submissions"
        description="Submission data could not be loaded right now. Check the backend connection and try again."
        action={<Button onClick={() => submissionsQuery.refetch()}>Retry</Button>}
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Assignments"
        title="Essay Review"
        description="Review submitted essays, request AI-assisted feedback, and record the final teacher-approved grade."
      />

      <div className="flex flex-wrap gap-3">
        <Select value={filterExamId} onChange={(event) => setFilterExamId(event.target.value)} placeholder="All assessments" className="w-56">
          {examOptions.map((submission) => (
            <option key={submission.examId} value={submission.examId}>
              {submission.examTitle}
            </option>
          ))}
        </Select>

        <Select value={filterStudentId} onChange={(event) => setFilterStudentId(event.target.value)} placeholder="All students" className="w-56">
          {studentOptions.map((submission) => (
            <option key={submission.studentId} value={submission.studentId}>
              {submission.studentName}
            </option>
          ))}
        </Select>

        {(filterExamId || filterStudentId) ? (
          <Button
            variant="ghost"
            onClick={() => {
              setFilterExamId("");
              setFilterStudentId("");
            }}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {filteredSubmissions.length === 0 ? (
        <EmptyState
          title="No submissions found"
          description={
            submissions.length === 0
              ? "Students have not submitted any essay answers yet."
              : "No submissions match the selected filters."
          }
        />
      ) : (
        <DataTable
          columns={createColumns()}
          rows={filteredSubmissions}
          getRowKey={(submission) => submission.id}
          onEdit={setSelectedSubmission}
        />
      )}

      <Modal
        open={Boolean(selectedSubmission)}
        title={selectedSubmission ? `${selectedSubmission.studentName} - ${selectedSubmission.examTitle}` : "Submission review"}
        description="Use AI for structured guidance, then save the teacher-approved final grade and release decision."
        onClose={() => setSelectedSubmission(null)}
      >
        {selectedSubmission ? (
          <div className="space-y-8">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <div className="rounded-2xl border border-slate-200 px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Class</p>
                <p className="mt-3 text-sm font-semibold text-slate-900">{selectedSubmission.className}</p>
              </div>
              <div className="rounded-2xl border border-slate-200 px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Subject</p>
                <p className="mt-3 text-sm font-semibold text-slate-900">{selectedSubmission.subjectName}</p>
              </div>
              <div className="rounded-2xl border border-slate-200 px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Submitted</p>
                <p className="mt-3 text-sm font-semibold text-slate-900">{formatDateTime(selectedSubmission.submittedAt)}</p>
              </div>
              <div className="rounded-2xl border border-slate-200 px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Maximum score</p>
                <p className="mt-3 text-sm font-semibold text-slate-900">{selectedSubmission.maximumScore}</p>
              </div>
            </div>

            {selectedSubmission.essayPrompt ? (
              <div className="space-y-2">
                <h3 className="text-lg font-semibold text-slate-950">Essay prompt</h3>
                <p className="rounded-2xl border border-slate-200 px-4 py-4 text-sm leading-7 text-slate-600">
                  {selectedSubmission.essayPrompt}
                </p>
              </div>
            ) : null}

            <div className="space-y-2">
              <h3 className="text-lg font-semibold text-slate-950">Student answer</h3>
              <div className="rounded-2xl border border-slate-200 px-4 py-4 text-sm leading-7 text-slate-600 whitespace-pre-wrap">
                {selectedSubmission.answerText}
              </div>
            </div>

            <form className="space-y-5 rounded-2xl border border-slate-200 px-5 py-5">
              <div className="flex items-center gap-2">
                <Bot className="h-5 w-5 text-brand-700" />
                <h3 className="text-lg font-semibold text-slate-950">AI review request</h3>
              </div>

              <FormField label="Rubric guidance" error={aiForm.formState.errors.rubricInstructions?.message ?? undefined}>
                <textarea
                  className="w-full resize-y rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
                  rows={4}
                  placeholder="Optional scoring rubric or class-specific criteria."
                  maxLength={2000}
                  {...aiForm.register("rubricInstructions")}
                />
              </FormField>

              <FormField label="Additional instructions" error={aiForm.formState.errors.additionalInstructions?.message ?? undefined}>
                <textarea
                  className="w-full resize-y rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
                  rows={3}
                  placeholder="Optional notes for the AI reviewer."
                  maxLength={1000}
                  {...aiForm.register("additionalInstructions")}
                />
              </FormField>

              <div className="flex flex-wrap gap-3">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={requestAiFeedback.isPending || requestSmartGrade.isPending}
                  onClick={aiForm.handleSubmit(async (values) => {
                    const updatedSubmission = await requestAiFeedback.mutateAsync({
                      id: selectedSubmission.id,
                      payload: values
                    });
                    setSelectedSubmission(updatedSubmission);
                  })}
                >
                  <Sparkles className="h-4 w-4" />
                  {requestAiFeedback.isPending ? "Generating..." : "Generate AI feedback"}
                </Button>

                <Button
                  type="button"
                  disabled={requestAiFeedback.isPending || requestSmartGrade.isPending}
                  onClick={aiForm.handleSubmit(async (values) => {
                    const updatedSubmission = await requestSmartGrade.mutateAsync({
                      id: selectedSubmission.id,
                      payload: values
                    });
                    setSelectedSubmission(updatedSubmission);
                  })}
                >
                  <BookCopy className="h-4 w-4" />
                  {requestSmartGrade.isPending ? "Generating..." : "Generate smart grade"}
                </Button>
              </div>
            </form>

            {selectedSubmission.aiReview ? (
              <div className="rounded-2xl border border-slate-200 px-5 py-5">
                <SubmissionAIInsights review={selectedSubmission.aiReview} maximumScore={selectedSubmission.maximumScore} />
              </div>
            ) : (
              <div className="rounded-2xl border border-dashed border-slate-300 px-5 py-6 text-sm text-slate-500">
                No AI review has been generated for this submission yet.
              </div>
            )}

            <form
              className="space-y-5 rounded-2xl border border-slate-200 px-5 py-5"
              onSubmit={reviewForm.handleSubmit(async (values) => {
                const updatedSubmission = await updateTeacherReview.mutateAsync({
                  id: selectedSubmission.id,
                  payload: values
                });
                setSelectedSubmission(updatedSubmission);
              })}
            >
              <div>
                <h3 className="text-lg font-semibold text-slate-950">Teacher override</h3>
                <p className="mt-1 text-sm text-slate-500">
                  AI guidance is advisory only. The teacher final score and release flag control what the student sees.
                </p>
              </div>

              <div className="grid gap-5 md:grid-cols-2">
                <FormField label="Final score" error={reviewForm.formState.errors.teacherFinalScore?.message ?? undefined}>
                  <Input
                    type="number"
                    min={0}
                    max={selectedSubmission.maximumScore}
                    step="0.01"
                    placeholder="Optional"
                    {...reviewForm.register("teacherFinalScore")}
                  />
                </FormField>

                <FormField label="Final grade" error={reviewForm.formState.errors.teacherFinalGrade?.message ?? undefined}>
                  <Input placeholder="Optional grade label" {...reviewForm.register("teacherFinalGrade")} />
                </FormField>
              </div>

              <FormField label="Teacher notes" error={reviewForm.formState.errors.teacherReviewNotes?.message ?? undefined}>
                <textarea
                  className="w-full resize-y rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
                  rows={4}
                  placeholder="Optional notes visible when released to the student."
                  maxLength={2000}
                  {...reviewForm.register("teacherReviewNotes")}
                />
              </FormField>

              <label className="flex items-start gap-3 rounded-2xl border border-slate-200 px-4 py-3">
                <input
                  type="checkbox"
                  className="mt-1 h-4 w-4 rounded border-slate-300 accent-brand-600"
                  {...reviewForm.register("isAiFeedbackReleasedToStudent")}
                />
                <div>
                  <p className="text-sm font-semibold text-slate-900">Release feedback to student</p>
                  <p className="mt-1 text-sm text-slate-500">
                    When enabled, the student can view the AI assessment, teacher notes, and final grading decision.
                  </p>
                </div>
              </label>

              <div className="flex justify-end">
                <Button type="submit" disabled={updateTeacherReview.isPending}>
                  {updateTeacherReview.isPending ? "Saving..." : "Save teacher review"}
                </Button>
              </div>
            </form>
          </div>
        ) : null}
      </Modal>
    </div>
  );
}
