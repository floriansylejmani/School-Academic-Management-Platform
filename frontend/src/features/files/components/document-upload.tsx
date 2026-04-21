"use client";

import { FileUp, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useUploadStudentDocument } from "@/features/files/hooks/use-files";

const ACCEPTED = ".pdf,.doc,.docx";
const MAX_MB = 20;
const ACCEPT_LABEL = "PDF, DOC, DOCX";

interface DocumentUploadProps {
  studentId: string;
}

export function DocumentUpload({ studentId }: DocumentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const mutation = useUploadStudentDocument(studentId);

  function handleFile(file: File) {
    if (file.size > MAX_MB * 1024 * 1024) {
      setError(`File must be smaller than ${MAX_MB} MB.`);
      return;
    }

    setError(null);
    mutation.mutate(file);
  }

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) {
      handleFile(file);
    }

    event.target.value = "";
  }

  function handleDrop(event: React.DragEvent) {
    event.preventDefault();
    setIsDragging(false);
    setError(null);

    const file = event.dataTransfer.files?.[0];
    if (file) {
      handleFile(file);
    }
  }

  useEffect(() => {
    if (!mutation.isError) return;
    setError((mutation.error as Error | null)?.message ?? "Failed to upload document.");
  }, [mutation.isError, mutation.error]);

  return (
    <div className="space-y-2">
      <div
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        className={[
          "relative flex flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed p-8 transition-colors",
          isDragging
            ? "border-brand-400 bg-brand-50"
            : "border-slate-200 bg-slate-50 hover:border-slate-300"
        ].join(" ")}
      >
        <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-white shadow-sm">
          <FileUp className="h-5 w-5 text-slate-500" />
        </div>

        <div className="text-center">
          <p className="text-sm font-medium text-slate-700">
            {isDragging ? "Drop to upload" : "Drag and drop a file here"}
          </p>
          <p className="mt-1 text-xs text-slate-400">{ACCEPT_LABEL} - Max {MAX_MB} MB</p>
        </div>

        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={mutation.isPending}
          onClick={() => inputRef.current?.click()}
          className="gap-2"
        >
          {mutation.isPending ? (
            <>
              <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-slate-700" />
              Uploading...
            </>
          ) : (
            <>
              <Upload className="h-3.5 w-3.5" />
              Browse file
            </>
          )}
        </Button>

        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED}
          className="sr-only"
          onChange={handleChange}
        />
      </div>

      {error ? <p className="text-sm text-rose-600">{error}</p> : null}
    </div>
  );
}
