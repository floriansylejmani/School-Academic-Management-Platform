"use client";

import { Download, File, FileText, Loader2, Trash2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { ConfirmDeleteDialog } from "@/components/ui/confirm-delete-dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { filesService } from "@/services/files.service";
import { useDeleteStudentDocument, useStudentDocuments } from "@/features/files/hooks/use-files";
import type { UploadedFileResponse } from "@/features/files/types/file.types";

interface DocumentListProps {
  studentId: string;
  canDelete?: boolean;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric"
  });
}

function FileIcon({ contentType }: { contentType: string }) {
  if (contentType === "application/pdf") {
    return <FileText className="h-5 w-5 text-rose-500" />;
  }
  return <File className="h-5 w-5 text-blue-500" />;
}

function DocumentRow({
  doc,
  canDelete,
  onDelete
}: {
  doc: UploadedFileResponse;
  canDelete: boolean;
  onDelete: (id: string) => Promise<void>;
}) {
  const [deleting, setDeleting] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  function handleRequestDelete() {
    setConfirmOpen(true);
  }

  function handleConfirmDelete() {
    setConfirmOpen(false);
    setDeleting(true);

    void onDelete(doc.id).finally(() => {
      setDeleting(false);
    });
  }

  return (
    <>
      <div className="flex items-center gap-3 rounded-2xl border border-slate-100 bg-white px-4 py-3">
        <FileIcon contentType={doc.contentType} />

        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-slate-800">{doc.originalFileName}</p>
          <p className="mt-0.5 text-xs text-slate-400">
            {formatSize(doc.fileSizeBytes)} · {formatDate(doc.uploadedAt)}
          </p>
        </div>

        <div className="flex shrink-0 items-center gap-1">
          {/* Download */}
          <a
            href={filesService.getDownloadUrl(doc.id)}
            download={doc.originalFileName}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-50 hover:text-slate-700"
            title="Download"
          >
            <Download className="h-4 w-4" />
          </a>

          {/* Delete */}
          {canDelete && (
            <button
              type="button"
              disabled={deleting}
              onClick={handleRequestDelete}
              className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50"
              title="Delete"
            >
              {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
            </button>
          )}
        </div>
      </div>

      <ConfirmDeleteDialog
        open={confirmOpen}
        title="Delete document"
        description={`This will permanently delete "${doc.originalFileName}".`}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={handleConfirmDelete}
        isPending={deleting}
      />
    </>
  );
}

export function DocumentList({ studentId, canDelete = false }: DocumentListProps) {
  const { data: docs, isLoading, isError, refetch } = useStudentDocuments(studentId);
  const deleteMutation = useDeleteStudentDocument(studentId);

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <div
            key={i}
            className="h-16 animate-pulse rounded-2xl border border-slate-100 bg-slate-50"
          />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <EmptyState
        title="Unable to load documents"
        description="Your document list could not be fetched right now. Check the backend connection and try again."
        action={<Button onClick={() => refetch()}>Retry</Button>}
      />
    );
  }

  if (!docs?.length) {
    return (
      <EmptyState
        title="No documents uploaded yet"
        description="Upload the first file to keep student records up to date."
      />
    );
  }

  return (
    <div className="space-y-2">
      {docs.map((doc) => (
        <DocumentRow
          key={doc.id}
          doc={doc}
          canDelete={canDelete}
          onDelete={(id) => deleteMutation.mutateAsync(id)}
        />
      ))}
    </div>
  );
}
