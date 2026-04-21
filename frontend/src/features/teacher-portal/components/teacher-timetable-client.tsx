"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useTeacherProfile } from "@/features/profile/hooks/use-profile";
import { useTimetable } from "@/features/timetable/hooks/use-timetable";
import type { DayOfWeek, TimetableEntry } from "@/features/timetable/types/timetable.types";

const DAYS_OF_WEEK: DayOfWeek[] = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday"
];

export function TeacherTimetableClient() {
  const teacherQuery = useTeacherProfile();
  const timetableQuery = useTimetable();
  const [selectedDay, setSelectedDay] = useState("");
  const [selectedClassId, setSelectedClassId] = useState("");

  const timetableEntries = timetableQuery.data?.items ?? [];

  const availableClasses = useMemo(() => {
    const classes = new Map<string, { id: string; name: string }>();

    timetableEntries.forEach((entry) => {
      classes.set(entry.classId, { id: entry.classId, name: entry.className });
    });

    return [...classes.values()].sort((left, right) => left.name.localeCompare(right.name));
  }, [timetableEntries]);

  const filteredEntries = useMemo(() => {
    return timetableEntries.filter((entry) => {
      if (selectedDay && entry.dayOfWeek !== selectedDay) {
        return false;
      }

      if (selectedClassId && entry.classId !== selectedClassId) {
        return false;
      }

      return true;
    });
  }, [selectedClassId, selectedDay, timetableEntries]);

  const groupedEntries = useMemo(() => {
    return DAYS_OF_WEEK.reduce<Record<DayOfWeek, TimetableEntry[]>>(
      (groups, day) => {
        groups[day] = filteredEntries
          .filter((entry) => entry.dayOfWeek === day)
          .sort((left, right) => left.startTime.localeCompare(right.startTime));
        return groups;
      },
      {
        Monday: [],
        Tuesday: [],
        Wednesday: [],
        Thursday: [],
        Friday: [],
        Saturday: [],
        Sunday: []
      }
    );
  }, [filteredEntries]);

  const daysToShow = useMemo(() => {
    if (selectedDay) {
      return [selectedDay as DayOfWeek];
    }

    return DAYS_OF_WEEK.filter((day) => groupedEntries[day].length > 0);
  }, [groupedEntries, selectedDay]);

  const todayName = DAYS_OF_WEEK[(new Date().getDay() + 6) % 7];
  const todayEntries = groupedEntries[todayName];
  const hasFilters = Boolean(selectedDay || selectedClassId);

  if (teacherQuery.isLoading || timetableQuery.isLoading) {
    return (
      <LoadingState
        title="Loading teacher timetable..."
        description="Preparing your assigned lessons and classroom schedule."
      />
    );
  }

  if (teacherQuery.isError || timetableQuery.isError) {
    return (
      <EmptyState
        title="Unable to load timetable"
        description="Teacher timetable data could not be loaded right now. Check the backend connection and try again."
        action={
          <Button
            onClick={() => {
              void teacherQuery.refetch();
              void timetableQuery.refetch();
            }}
          >
            Retry
          </Button>
        }
      />
    );
  }

  if (!teacherQuery.data?.id) {
    return (
      <EmptyState
        title="Teacher profile unavailable"
        description="Your teacher profile could not be resolved. Please contact an administrator."
      />
    );
  }

  if (timetableEntries.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="My Schedule"
          title="My Timetable"
          description="Review the weekly schedule for the classes and subjects assigned to your teacher account."
        />
        <EmptyState
          title="No timetable entries assigned"
          description="Your account does not have any timetable entries yet. Ask an administrator to assign your classes and subjects."
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="My Schedule"
        title="My Timetable"
        description={`Weekly schedule for ${teacherQuery.data.fullName}. Only lessons assigned to your teacher account are shown here.`}
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <Card className="p-5">
          <p className="text-sm text-slate-500">Lessons this week</p>
          <p className="mt-3 text-3xl font-semibold tabular-nums text-slate-950">{timetableEntries.length}</p>
          <p className="mt-2 text-sm text-slate-500">Across your assigned classes and subjects.</p>
        </Card>

        <Card className="p-5">
          <p className="text-sm text-slate-500">Classes covered</p>
          <p className="mt-3 text-3xl font-semibold tabular-nums text-slate-950">{availableClasses.length}</p>
          <p className="mt-2 text-sm text-slate-500">Distinct classes in your current timetable scope.</p>
        </Card>

        <Card className="p-5">
          <p className="text-sm text-slate-500">Lessons today</p>
          <p className="mt-3 text-3xl font-semibold tabular-nums text-slate-950">{todayEntries.length}</p>
          <p className="mt-2 text-sm text-slate-500">{todayName} schedule based on your assigned timetable entries.</p>
        </Card>
      </div>

      <div className="flex flex-wrap gap-3">
        <Select
          value={selectedDay}
          onChange={(event) => setSelectedDay(event.target.value)}
          placeholder="All days"
          className="w-44"
        >
          {DAYS_OF_WEEK.map((day) => (
            <option key={day} value={day}>
              {day}
            </option>
          ))}
        </Select>

        <Select
          value={selectedClassId}
          onChange={(event) => setSelectedClassId(event.target.value)}
          placeholder="All classes"
          className="w-48"
        >
          {availableClasses.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </Select>

        {hasFilters ? (
          <Button
            variant="ghost"
            onClick={() => {
              setSelectedDay("");
              setSelectedClassId("");
            }}
          >
            Clear filters
          </Button>
        ) : null}
      </div>

      {daysToShow.length === 0 ? (
        <EmptyState
          title="No timetable entries found"
          description="No timetable entries match the selected filters. Try adjusting or clearing them."
          action={
            hasFilters ? (
              <Button
                onClick={() => {
                  setSelectedDay("");
                  setSelectedClassId("");
                }}
              >
                Clear filters
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-6">
          {daysToShow.map((day) => (
            <Card key={day} className="overflow-hidden">
              <div className="border-b border-slate-200 bg-slate-50 px-6 py-4">
                <div className="flex items-center justify-between gap-3">
                  <h3 className="font-semibold text-slate-900">{day}</h3>
                  <span className="text-sm text-slate-500">
                    {groupedEntries[day].length} lesson{groupedEntries[day].length === 1 ? "" : "s"}
                  </span>
                </div>
              </div>

              <div className="divide-y divide-slate-100">
                {groupedEntries[day].map((entry) => (
                  <div key={entry.id} className="grid gap-4 px-6 py-4 md:grid-cols-[auto_1fr_auto] md:items-center">
                    <div className="w-28 text-center">
                      <p className="text-sm font-medium text-slate-700">{entry.startTime}</p>
                      <p className="text-xs text-slate-400">{entry.endTime}</p>
                    </div>

                    <div>
                      <p className="font-semibold text-slate-900">{entry.subjectName}</p>
                      <p className="mt-1 text-sm text-slate-500">{entry.className}</p>
                    </div>

                    <div className="text-left md:text-right">
                      {entry.roomNumber ? (
                        <span className="inline-flex items-center rounded-full bg-brand-50 px-3 py-1 text-xs font-semibold text-brand-700">
                          Room {entry.roomNumber}
                        </span>
                      ) : (
                        <span className="text-sm text-slate-400">Room not set</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
