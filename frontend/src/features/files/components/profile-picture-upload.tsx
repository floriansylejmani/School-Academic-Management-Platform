"use client";

import { Camera, User2 } from "lucide-react";
import Image from "next/image";
import { useEffect, useRef, useState } from "react";
import { useUploadProfilePicture } from "@/features/files/hooks/use-files";

const ACCEPTED = ".jpg,.jpeg,.png,.webp";
const MAX_MB   = 5;

interface ProfilePictureUploadProps {
  currentUrl?: string | null;
  userId?: string;          // Admins can pass a target userId
  onUploaded?: (url: string) => void;
  size?: number;            // px, default 96
}

export function ProfilePictureUpload({
  currentUrl,
  userId,
  onUploaded,
  size = 96
}: ProfilePictureUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const mutation = useUploadProfilePicture(onUploaded);

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setError(null);

    if (file.size > MAX_MB * 1024 * 1024) {
      setError(`File must be smaller than ${MAX_MB} MB.`);
      return;
    }

    setPreview(URL.createObjectURL(file));
    mutation.mutate({ file, userId });

    // Reset input so the same file can be re-selected after an error
    e.target.value = "";
  }

  const displayUrl = preview ?? currentUrl;

  useEffect(() => {
    if (!mutation.isError) return;
    setError((mutation.error as Error | null)?.message ?? "Failed to upload profile picture.");
    // Roll back preview so the avatar doesn't look like the upload succeeded.
    setPreview(null);
  }, [mutation.isError, mutation.error]);

  return (
    <div className="space-y-2">
      <div className="relative" style={{ width: size, height: size }}>
        {/* Avatar */}
        <div
          className="overflow-hidden rounded-full border-2 border-slate-200 bg-slate-100"
          style={{ width: size, height: size }}
        >
          {displayUrl ? (
            <Image
              src={displayUrl}
              alt="Profile picture"
              width={size}
              height={size}
              className="h-full w-full object-cover"
              unoptimized
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <User2 className="text-slate-400" style={{ width: size * 0.45, height: size * 0.45 }} />
            </div>
          )}
        </div>

        {/* Upload overlay button */}
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={mutation.isPending}
          className="absolute bottom-0 right-0 flex h-8 w-8 items-center justify-center rounded-full border border-slate-200 bg-white shadow-sm transition-colors hover:bg-slate-50 disabled:opacity-50"
          title="Change profile picture"
        >
          {mutation.isPending ? (
            <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-brand-600" />
          ) : (
            <Camera className="h-3.5 w-3.5 text-slate-600" />
          )}
        </button>

        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED}
          className="sr-only"
          onChange={handleFileChange}
        />
      </div>

      {error ? <p className="text-sm text-rose-600">{error}</p> : null}
    </div>
  );
}
