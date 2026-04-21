"use client";

import { useState } from "react";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingState } from "@/components/ui/loading-state";
import { useAuthStore } from "@/store/auth.store";
import { NotificationsInbox } from "@/features/notifications/components/notifications-inbox";
import { ParentChildSwitcher } from "@/features/parent-portal/components/parent-child-switcher";
import { useParentChildSelection } from "@/features/parent-portal/hooks/use-parent-child-selection";
import { useParentChildren } from "@/features/profile/hooks/use-profile";

type ChildFilterMode = "all" | "child";

export function ParentNotificationsClient() {
  const { user } = useAuthStore();
  const childrenQuery = useParentChildren(user?.id);
  const children = childrenQuery.data?.items ?? [];
  const { activeChild, activeChildId, setSelectedChildId } = useParentChildSelection(children);

  // "all" shows every notification for the parent; "child" filters by selected child
  const [filterMode, setFilterMode] = useState<ChildFilterMode>("all");
  const isFiltered = filterMode === "child" && children.length > 0;

  if (childrenQuery.isLoading) {
    return <LoadingState title="Loading..." description="Fetching child profiles." />;
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

  const notificationParams = isFiltered && activeChildId
    ? { studentId: activeChildId }
    : undefined;

  const emptyDescription = isFiltered
    ? `No notifications for ${activeChild?.fullName ?? "this child"} at this time.`
    : "You have no notifications yet. Check back after the next school update.";

  return (
    <NotificationsInbox
      eyebrow="Notifications"
      title="Inbox"
      description={
        isFiltered
          ? `Showing notifications related to ${activeChild?.fullName ?? "the selected child"}.`
          : "Updates about your children's attendance, assessments, fees, and academic results."
      }
      emptyTitle="No notifications"
      emptyDescription={emptyDescription}
      params={notificationParams}
      headerSlot={
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex gap-2">
            {(["all", "child"] as ChildFilterMode[]).map((mode) => (
              <button
                key={mode}
                type="button"
                onClick={() => setFilterMode(mode)}
                className={`rounded-2xl px-4 py-2 text-sm font-medium transition ${
                  filterMode === mode
                    ? "bg-brand-600 text-white shadow-sm"
                    : "bg-slate-100 text-slate-700 hover:bg-slate-200"
                }`}
              >
                {mode === "all" ? "All children" : "By child"}
              </button>
            ))}
          </div>

          {filterMode === "child" ? (
            <ParentChildSwitcher
              students={children}
              value={activeChildId}
              onChange={setSelectedChildId}
              className="w-56"
            />
          ) : null}
        </div>
      }
    />
  );
}
