"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { LoadingState } from "@/components/ui/loading-state";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useStudentProfile } from "@/features/profile/hooks/use-profile";
import { useStudentExams } from "@/features/student-portal/hooks/use-student-portal";
import { SubmissionAIInsights } from "@/features/submissions/components/submission-ai-insights";
import { createSubmissionSchema, type CreateSubmissionFormValues } from "@/features/submissions/schemas/submission.schema";
import { useCreateSubmission, useSubmissions } from "@/features/submissions/hooks/use-submissions";

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

export function StudentSubmissionsClient() {
  const profileQuery = useStudentProfile();
  const classId = profileQuery.data?.classId ?? undefined;
  const examsQuery = useStudentExams(classId);
  const submissionsQuery = useSubmissions({ pageNumber: 1, pageSize: 100 });
  const createSubmission = useCreateSubmission();
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const form = useForm<CreateSubmissionFormValues>({
    resolver: zodResolver(createSubmissionSchema),
    defaultValues: {
      examId: "",
      essayPrompt: null,
      answerText: ""
    }
  });

  const exams = examsQuery.data?.items ?? [];
  const submissions = useMemo(
    () =>
      [...(submissionsQuery.data?.items ?? [])].sort(
        (left, right) => new Date(right.submittedAt).getTime() - new Date(left.submittedAt).getTime()
      ),
    [submissionsQuery.data]
  );
  const submittedExamIds = new Set(submissions.map((submission) => submission.examId));
  const availableExams = exams.filter((exam) => !submittedExamIds.has(exam.id));

  if (profileQuery.isLoading || examsQuery.isLoading || submissionsQuery.isLoading) {
    return <LoadingState title="Loading submissions..." description="Preparing your essay assessments and feedback." />;
  }

  if (profileQuery.isError || examsQuery.isError || submissionsQuery.isError) {
    return (
      <EmptyState
        title="Unable to load submissions"
        description="Your submissions or essay exams could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void profileQuery.refetch();
              void examsQuery.refetch();
              void submissionsQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Assignments"
        title="Essay Submissions"
        description="Submit written responses and review teacher feedback, including AI-assisted commentary where available."
        actionLabel="New submission"
        onAction={() => setIsCreateOpen(true)}
        actionDisabled={availableExams.length === 0}
      />

      {submissions.length === 0 ? (
        <EmptyState
          title="No submissions yet"
          description={availableExams.length > 0 ? "Submit your first essay answer to start receiving feedback." : "There are no essay exams available for submission right now."}
          action={availableExams.length > 0 ? <Button onClick={() => setIsCreateOpen(true)}>New submission</Button> : undefined}
        />
      ) : (
        <div className="space-y-4">
          {submissions.map((submission) => (
            <Card key={submission.id} className="p-6">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-lg font-semibold text-slate-950">{submission.examTitle}</p>
                  <p className="mt-1 text-sm text-slate-500">
                    {submission.subjectName} · {submission.className} · Submitted {formatDateTime(submission.submittedAt)}
                  </p>
                </div>
                <span
                  className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ${
                    submission.isAiFeedbackReleasedToStudent ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-600"
                  }`}
                >
                  {submission.isAiFeedbackReleasedToStudent ? "Feedback released" : "Awaiting teacher release"}
                </span>
              </div>

              {submission.essayPrompt ? (
                <div className="mt-5 space-y-2">
                  <h3 className="text-sm font-semibold text-slate-900">Essay prompt</h3>
                  <p className="text-sm leading-7 text-slate-600">{submission.essayPrompt}</p>
                </div>
              ) : null}

              <div className="mt-5 space-y-2">
                <h3 className="text-sm font-semibold text-slate-900">Your answer</h3>
                <div className="text-sm leading-7 text-slate-600 whitespace-pre-wrap">{submission.answerText}</div>
              </div>

              {submission.isAiFeedbackReleasedToStudent ? (
                <div className="mt-6 space-y-6 border-t border-slate-200 pt-6">
                  <div className="grid gap-4 sm:grid-cols-3">
                    <div className="rounded-2xl border border-slate-200 px-4 py-4">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Teacher score</p>
                      <p className="mt-3 text-2xl font-semibold text-slate-950">
                        {submission.teacherFinalScore ?? "Pending"} / {submission.maximumScore}
                      </p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 px-4 py-4">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Teacher grade</p>
                      <p className="mt-3 text-2xl font-semibold text-slate-950">{submission.teacherFinalGrade ?? "Pending"}</p>
                    </div>
                    <div className="rounded-2xl border border-slate-200 px-4 py-4">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Reviewed</p>
                      <p className="mt-3 text-sm font-semibold text-slate-950">
                        {submission.reviewedAt ? formatDateTime(submission.reviewedAt) : "Pending"}
                      </p>
                    </div>
                  </div>

                  {submission.teacherReviewNotes ? (
                    <div className="space-y-2">
                      <h3 className="text-sm font-semibold text-slate-900">Teacher notes</h3>
                      <p className="text-sm leading-7 text-slate-600">{submission.teacherReviewNotes}</p>
                    </div>
                  ) : null}

                  {submission.aiReview ? (
                    <SubmissionAIInsights review={submission.aiReview} maximumScore={submission.maximumScore} />
                  ) : null}
                </div>
              ) : null}
            </Card>
          ))}
        </div>
      )}

      <Modal
        open={isCreateOpen}
        title="Submit essay answer"
        description="Choose an exam and send your essay response for teacher review."
        onClose={() => setIsCreateOpen(false)}
      >
        {availableExams.length === 0 ? (
          <EmptyState
            title="No essay exams available"
            description="You have already submitted all available essay exams for your class."
          />
        ) : (
          <form
            className="space-y-5"
            onSubmit={form.handleSubmit(async (values) => {
              await createSubmission.mutateAsync({
                examId: values.examId,
                essayPrompt: values.essayPrompt,
                answerText: values.answerText
              });
              form.reset({
                examId: "",
                essayPrompt: null,
                answerText: ""
              });
              setIsCreateOpen(false);
            })}
          >
            <FormField label="Exam" error={form.formState.errors.examId?.message}>
              <Select {...form.register("examId")} placeholder="Select an exam">
                {availableExams.map((exam) => (
                  <option key={exam.id} value={exam.id}>
                    {exam.title} - {exam.subjectName}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Essay prompt (optional override)" error={form.formState.errors.essayPrompt?.message ?? undefined}>
              <textarea
                className="w-full resize-y rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
                rows={4}
                placeholder="Optional prompt text if you want to preserve the exact question with your answer."
                maxLength={2000}
                {...form.register("essayPrompt")}
              />
            </FormField>

            <FormField label="Answer" error={form.formState.errors.answerText?.message}>
              <textarea
                className="w-full resize-y rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-1"
                rows={10}
                placeholder="Write your essay answer here."
                maxLength={20000}
                {...form.register("answerText")}
              />
            </FormField>

            <div className="flex justify-end">
              <Button type="submit" disabled={createSubmission.isPending}>
                {createSubmission.isPending ? "Submitting..." : "Submit answer"}
              </Button>
            </div>
          </form>
        )}
      </Modal>
    </div>
  );
}
