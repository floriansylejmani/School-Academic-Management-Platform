export interface UploadedFileResponse {
  id: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  entityType: string;
  entityId: string;
  downloadUrl: string;
  uploadedAt: string;
}

export type FileEntityType = "ProfilePicture" | "StudentDocument";
