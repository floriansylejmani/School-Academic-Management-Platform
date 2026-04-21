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
import { TimetableForm } from "@/features/timetable/components/timetable-form";
import {
  useTimetable,
  useCreateTimetableEntry,
  useUpdateTimetableEntry,
  useDeleteTimetableEntry
} from "@/features/timetable/hooks/use-timetable";
import type { TimetableEntry, CreateTimetableEntryDto, UpdateTimetableEntryDto } from "@/features/timetable/types/timetable.types";

const DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"] as const;

const columns: DataTableColumn<TimetableEntry>[] = [
  {
    key: "day",
    header: "Day",
    render: (entry) => <span className="font-medium text-slate-900">{entry.dayOfWeek}</span>
  },
  {
    key: "time",
    header: "Time",
    render: (entry) => (
      <span className="text-slate-600">
        {entry.startTime} – {entry.endTime}
      </span>
    )
  },
  {
    key: "class",
    header: "Class",
    render: (entry) => entry.className
  },
  {
    key: "subject",
    header: "Subject",
    render: (entry) => entry.subjectName
  },
  {
    key: "teacher",
    header: "Teacher",
    render: (entry) => entry.teacherName
  },
  {
    key: "room",
    header: "Room",
    render: (entry) => entry.roomNumber ?? <span className="text-slate-400">—</span>
  }
];

export function TimetableAdminClient() {
  const timetableQuery = useTimetable();
  const { data: classesData } = useClasses();
  const createEntry = useCreateTimetableEntry();
  const updateEntry = useUpdateTimetableEntry();
  const deleteEntry = useDeleteTimetableEntry();

  const [editingEntry, setEditingEntry] = useState<TimetableEntry | null>(null);
  const [deletingEntry, setDeletingEntry] = useState<TimetableEntry | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [filterClassId, setFilterClassId] = useState("");
  const [filterDay, setFilterDay] = useState("");

  const classes = classesData?.items ?? [];

  const entries = useMemo(() => {
    const all = timetableQuery.data?.items ?? [];
    return all.filter((entry) => {
      if (filterClassId && entry.classId !== filterClassId) return false;
      if (filterDay && entry.dayOfWeek !== filterDay) return false;
      return true;
    });
  }, [timetableQuery.data, filterClassId, filterDay]);

  if (timetableQuery.isLoading) {
    return (
      <LoadingState
        title="Loading timetable..."
        description="Fetching schedule entries from the API."
      />
    );
  }

  if (timetableQuery.isError) {
    return (
      <EmptyState
        title="Unable to load timetable"
        description="The schedule could not be fetched right now. Check the backend connection and try again."
        action={<Button onClick={() => timetableQuery.refetch()}>Retry</Button>}
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Scheduling"
        title="Timetable"
        description="Configure class schedules, teacher allocations, and subject periods across the academic week."
        actionLabel="Add entry"
        onAction={() => setIsCreateOpen(true)}
      />

      <div className="flex flex-wrap gap-3">
        <Select
          value={filterClassId}
          onChange={(e) => setFilterClassId(e.target.value)}
          placeholder="All classes"
          className="w-48"
        >
          {classes.map((cls) => (
            <option key={cls.id} value={cls.id}>
              {cls.name} {cls.section}
            </option>
          ))}
        </Select>

        <Select
          value={filterDay}
          onChange={(e) => setFilterDay(e.target.value)}
          placeholder="All days"
          className="w-44"
        >
          {DAYS_OF_WEEK.map((day) => (
            <option key={day} value={day}>
              {day}
            </option>
          ))}
        </Select>

        {(filterClassId || filterDay) ? (
          <Button
            variant="ghost"
            onClick={() => {
              setFilterClassId("");
              setFilterDay("");
            }}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {entries.length === 0 ? (
        <EmptyState
          title="No timetable entries found"
          description={
            filterClassId || filterDay
              ? "No entries match the selected filters. Try adjusting or clearing them."
              : "Add the first schedule entry to start building the class timetable."
          }
          action={
            !filterClassId && !filterDay ? (
              <Button onClick={() => setIsCreateOpen(true)}>Add entry</Button>
            ) : undefined
          }
        />
      ) : (
        <DataTable
          columns={columns}
          rows={entries}
          getRowKey={(entry) => entry.id}
          onEdit={setEditingEntry}
          onDelete={setDeletingEntry}
        />
      )}

      <Modal
        open={isCreateOpen}
        title="Add timetable entry"
        description="Schedule a subject for a class with an assigned teacher."
        onClose={() => setIsCreateOpen(false)}
      >
        <TimetableForm
          mode="create"
          isSubmitting={createEntry.isPending}
          onSubmit={async (payload) => {
            try {
              await createEntry.mutateAsync(payload as CreateTimetableEntryDto);
              setIsCreateOpen(false);
            } catch {
              // error handled via toast in useCreateTimetableEntry
            }
          }}
        />
      </Modal>

      <Modal
        open={Boolean(editingEntry)}
        title="Edit timetable entry"
        description="Update the schedule, teacher, or room assignment."
        onClose={() => setEditingEntry(null)}
      >
        <TimetableForm
          mode="edit"
          initialValues={editingEntry}
          isSubmitting={updateEntry.isPending}
          onSubmit={async (payload) => {
            if (!editingEntry) return;
            try {
              await updateEntry.mutateAsync({ id: editingEntry.id, payload: payload as UpdateTimetableEntryDto });
              setEditingEntry(null);
            } catch {
              // error handled via toast in useUpdateTimetableEntry
            }
          }}
        />
      </Modal>

      <ConfirmDeleteDialog
        open={Boolean(deletingEntry)}
        title="Delete timetable entry"
        description={`This will permanently remove the ${deletingEntry?.dayOfWeek ?? ""} ${deletingEntry?.startTime ?? ""} entry for ${deletingEntry?.className ?? "this class"}.`}
        onCancel={() => setDeletingEntry(null)}
        onConfirm={async () => {
          if (!deletingEntry) return;
          try {
            await deleteEntry.mutateAsync(deletingEntry.id);
            setDeletingEntry(null);
          } catch {
            // error handled via toast in useDeleteTimetableEntry
          }
        }}
        isPending={deleteEntry.isPending}
      />
    </div>
  );
}
