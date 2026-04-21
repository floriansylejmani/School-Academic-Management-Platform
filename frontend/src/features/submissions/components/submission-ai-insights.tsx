"use client";

import type { SubmissionAIReview } from "@/features/submissions/types/submissions.types";

function scoreTone(score: number) {
  if (score >= 85) {
    return "text-emerald-700 bg-emerald-50";
  }

  if (score >= 70) {
    return "text-blue-700 bg-blue-50";
  }

  if (score >= 55) {
    return "text-amber-700 bg-amber-50";
  }

  return "text-rose-700 bg-rose-50";
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function InsightList({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="space-y-2">
      <h4 className="text-sm font-semibold text-slate-900">{title}</h4>
      <ul className="space-y-2 text-sm leading-6 text-slate-600">
        {items.map((item) => (
          <li key={item} className="flex gap-2">
            <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-slate-400" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function SubmissionAIInsights({ review, maximumScore }: { review: SubmissionAIReview; maximumScore: number }) {
  const scoreItems = [
    { label: "Grammar", value: review.grammarScore },
    { label: "Clarity", value: review.clarityScore },
    { label: "Structure", value: review.structureScore },
    { label: "Content", value: review.contentScore }
  ];

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-slate-950">AI assessment</h3>
          <p className="mt-1 text-sm text-slate-500">
            {review.mode === "SmartGrade" ? "Smart grading guidance" : "Feedback review"} generated on{" "}
            {formatDateTime(review.generatedAt)}
          </p>
        </div>
        <div className="inline-flex items-center rounded-full bg-brand-50 px-3 py-1 text-sm font-semibold text-brand-700">
          Suggested score {review.overallSuggestedScore} / {maximumScore}
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {scoreItems.map((item) => (
          <div key={item.label} className="rounded-2xl border border-slate-200 px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{item.label}</p>
            <div className="mt-3 flex items-center justify-between gap-3">
              <p className="text-2xl font-semibold text-slate-950">{item.value}</p>
              <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ${scoreTone(item.value)}`}>
                /100
              </span>
            </div>
          </div>
        ))}
      </div>

      <div className="space-y-2">
        <h4 className="text-sm font-semibold text-slate-900">Summary</h4>
        <p className="text-sm leading-7 text-slate-600">{review.summaryFeedback}</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <InsightList title="Strengths" items={review.strengths} />
        <InsightList title="Weaknesses" items={review.weaknesses} />
        <InsightList title="Improvements" items={review.improvements} />
      </div>

      <div className="space-y-3">
        <h4 className="text-sm font-semibold text-slate-900">Rubric breakdown</h4>
        <div className="space-y-3">
          {review.rubricBreakdown.map((item) => (
            <div key={`${item.criterion}-${item.feedback}`} className="rounded-2xl border border-slate-200 px-4 py-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-slate-900">{item.criterion}</p>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{item.feedback}</p>
                </div>
                <span className="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700">
                  {item.score} / {item.maxScore}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {review.safetyNotes ? (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          {review.safetyNotes}
        </div>
      ) : null}
    </div>
  );
}
