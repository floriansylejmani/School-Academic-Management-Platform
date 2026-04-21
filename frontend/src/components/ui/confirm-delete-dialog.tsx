"use client";

import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";

export function ConfirmDeleteDialog({
  open,
  title,
  description,
  onCancel,
  onConfirm,
  isPending
}: {
  open: boolean;
  title: string;
  description: string;
  onCancel: () => void;
  onConfirm: () => void;
  isPending?: boolean;
}) {
  return (
    <Modal open={open} title={title} description={description} onClose={onCancel} size="md">
      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onCancel} disabled={isPending}>
          Cancel
        </Button>
        <Button className="bg-rose-600 hover:bg-rose-700" onClick={onConfirm} disabled={isPending}>
          {isPending ? "Deleting..." : "Delete"}
        </Button>
      </div>
    </Modal>
  );
}
