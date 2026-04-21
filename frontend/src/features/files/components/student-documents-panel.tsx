"use client";

import { DocumentList } from "@/features/files/components/document-list";
import { DocumentUpload } from "@/features/files/components/document-upload";

interface StudentDocumentsPanelProps {
  studentId: string;
  canDelete?: boolean;
}

export function StudentDocumentsPanel({ studentId, canDelete = false }: StudentDocumentsPanelProps) {
  return (
    <div className="space-y-4">
      <DocumentUpload studentId={studentId} />
      <DocumentList studentId={studentId} canDelete={canDelete} />
    </div>
  );
}
