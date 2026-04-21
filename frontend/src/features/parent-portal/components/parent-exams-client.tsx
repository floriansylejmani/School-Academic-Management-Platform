"use client";

import { useMemo } from "react";
import { Card } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { useAuthStore } from "@/store/auth.store";
import { useParentChildren } from "@/features/profile/hooks/use-profile";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useChildExams } from "@/features/parent-portal/hooks/use-parent-portal";
import type { Exam } from "@/features/exams/types/exams.types";

const columns: DataTableColumn<Exam>[] = [
  {
    key: "title",
    header: "Exam",
    render: (exam) => <span className="font-semibold text-slate-900">{exam.title}</span>
  },
  {
    key: "subject",
    header: "Subject",
    render: (exam) => exam.subjectName
  },
  {
    key: "date",
    header: "Date",
    render: (exam) => exam.examDate
  },
  {
    key: "marks",
    header: "Total Marks",
    render: (exam) => <span className="font-medium text-slate-700">{exam.totalMarks}</span>
  }
];

export function ParentExamsClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);
  const examsQuery = useChildExams(activeChild?.classId ?? undefined);

  const upcomingExams = useMemo(() => {
    const today = new Date();

    return [...(examsQuery.data?.items ?? [])]
      .filter((exam) => new Date(exam.examDate) >= new Date(today.toDateString()))
      .sort((left, right) => new Date(left.examDate).getTime() - new Date(right.examDate).getTime());
  }, [examsQuery.data?.items]);

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading children..." description="Fetching linked student data." />;
  }

  if (childrenQuery.isError) {
    return (
      <EmptyState
        title="Unable to load children"
        description="Linked student profiles could not be loaded right now."
      />
    );
  }

  if (children.length === 0) {
    return (
      <EmptyState
        title="No child linked"
        description="No student is linked to your parent account."
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Upcoming Exams"
        title="Exam Schedule"
        description={`Scheduled assessments for ${activeChild?.fullName ?? "your child"} — dates, subjects, and class details.`}
      />

      <div className="flex flex-wrap gap-3">
        <ParentChildSwitcher
          students={children}
          value={activeChildId}
          onChange={setSelectedChildId}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Upcoming exams</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">{upcomingExams.length}</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Subjects covered</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {new Set(upcomingExams.map((exam) => exam.subjectId)).size}
          </p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-slate-500">Next exam</p>
          <p className="mt-3 text-2xl font-semibold text-slate-950">
            {upcomingExams[0]?.examDate ?? "-"}
          </p>
        </Card>
      </div>

      {examsQuery.isLoading ? (
        <LoadingState title="Loading exams..." description="Fetching scheduled exams." />
      ) : examsQuery.isError ? (
        <EmptyState
          title="Unable to load exams"
          description="Exam data could not be loaded for the selected child."
        />
      ) : upcomingExams.length === 0 ? (
        <EmptyState
          title="No upcoming exams"
          description="There are no scheduled exams for the selected child."
        />
      ) : (
        <DataTable columns={columns} rows={upcomingExams} getRowKey={(exam) => exam.id} />
      )}
    </div>
  );
}
