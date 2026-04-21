"use client";

import { useState } from "react";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { useStudentProfile } from "@/features/profile/hooks/use-profile";
import { useStudentTimetable } from "@/features/student-portal/hooks/use-student-portal";

const DAYS_OF_WEEK = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday"
] as const;

export function StudentTimetableClient() {
  const profileQuery = useStudentProfile();
  const classId = profileQuery.data?.classId ?? undefined;

  const timetableQuery = useStudentTimetable(classId);
  const [selectedDay, setSelectedDay] = useState("");

  if (profileQuery.isLoading || timetableQuery.isLoading) {
    return <LoadingState title="Loading timetable..." description="Fetching your class schedule." />;
  }

  if (profileQuery.isError) {
    return (
      <EmptyState
        title="Profile unavailable"
        description="Unable to load your student profile. Contact the admin."
      />
    );
  }

  if (!classId) {
    return (
      <EmptyState
        title="No class assigned"
        description="You have not been assigned to a class yet. Contact your admin."
      />
    );
  }

  const allEntries = timetableQuery.data?.items ?? [];

  const filteredEntries = selectedDay
    ? allEntries.filter((e) => e.dayOfWeek === selectedDay)
    : allEntries;

  const groupedByDay = DAYS_OF_WEEK.reduce<Record<string, typeof allEntries>>(
    (acc, day) => {
      acc[day] = filteredEntries
        .filter((e) => e.dayOfWeek === day)
        .sort((a, b) => a.startTime.localeCompare(b.startTime));
      return acc;
    },
    {}
  );

  const daysToShow = selectedDay ? [selectedDay] : DAYS_OF_WEEK.filter((d) => groupedByDay[d].length > 0);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="My Schedule"
        title="My Timetable"
        description={`Weekly schedule for ${profileQuery.data?.className ?? "your class"}. All subjects, teachers, and room numbers are listed below.`}
      />

      <div className="flex items-center gap-3">
        <Select
          value={selectedDay}
          onChange={(e) => setSelectedDay(e.target.value)}
          placeholder="All days"
          className="w-44"
        >
          {DAYS_OF_WEEK.map((day) => (
            <option key={day} value={day}>
              {day}
            </option>
          ))}
        </Select>
      </div>

      {daysToShow.length === 0 ? (
        <EmptyState
          title="No schedule entries"
          description="No timetable entries have been added for your class yet."
        />
      ) : (
        <div className="space-y-6">
          {daysToShow.map((day) => {
            const entries = groupedByDay[day];
            if (entries.length === 0) return null;
            return (
              <Card key={day} className="overflow-hidden">
                <div className="border-b border-slate-200 bg-slate-50 px-6 py-4">
                  <h3 className="font-semibold text-slate-900">{day}</h3>
                </div>
                <div className="divide-y divide-slate-100">
                  {entries.map((entry) => (
                    <div
                      key={entry.id}
                      className="grid grid-cols-[auto_1fr_auto] items-center gap-4 px-6 py-4"
                    >
                      <div className="w-28 text-center">
                        <p className="text-sm font-medium text-slate-700">
                          {entry.startTime}
                        </p>
                        <p className="text-xs text-slate-400">{entry.endTime}</p>
                      </div>
                      <div>
                        <p className="font-semibold text-slate-900">{entry.subjectName}</p>
                        <p className="text-sm text-slate-500">{entry.teacherName}</p>
                      </div>
                      <div className="text-right">
                        {entry.roomNumber ? (
                          <span className="inline-flex items-center rounded-full bg-brand-50 px-3 py-1 text-xs font-semibold text-brand-700">
                            {entry.roomNumber}
                          </span>
                        ) : null}
                      </div>
                    </div>
                  ))}
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
