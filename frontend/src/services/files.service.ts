import { apiClient } from "@/services/apiClient";
import { requireApiData } from "@/services/service-helpers";
import type { ApiResponse } from "@/types/common";
import type { UploadedFileResponse } from "@/features/files/types/file.types";

export const filesService = {
  /**
   * Upload a profile picture.
   * Pass userId only if an Admin is uploading on behalf of another user.
   */
  async uploadProfilePicture(file: File, userId?: string): Promise<UploadedFileResponse> {
    const formData = new FormData();
    formData.append("file", file);
    const params = userId ? { userId } : undefined;
    const response = await apiClient.post<ApiResponse<UploadedFileResponse>>(
      "/files/profile-picture",
      formData,
      { headers: { "Content-Type": "multipart/form-data" }, params }
    );
    return requireApiData(response.data.data);
  },

  async uploadStudentDocument(studentId: string, file: File): Promise<UploadedFileResponse> {
    const formData = new FormData();
    formData.append("file", file);
    const response = await apiClient.post<ApiResponse<UploadedFileResponse>>(
      `/files/students/${studentId}/documents`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return requireApiData(response.data.data);
  },

  async getStudentDocuments(studentId: string): Promise<UploadedFileResponse[]> {
    const response = await apiClient.get<ApiResponse<UploadedFileResponse[]>>(
      `/files/students/${studentId}/documents`
    );
    return requireApiData(response.data.data) ?? [];
  },

  async deleteStudentDocument(studentId: string, documentId: string): Promise<void> {
    await apiClient.delete(`/files/students/${studentId}/documents/${documentId}`);
  },

  /** Returns the full download URL to use in an <a href> or fetch call. */
  getDownloadUrl(fileId: string): string {
    return `${apiClient.defaults.baseURL ?? ""}/files/${fileId}/download`;
  }
};
