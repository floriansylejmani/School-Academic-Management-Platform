import { apiClient } from "@/services/apiClient";

export type ReportPdfType = "students" | "attendance" | "fees";

export interface ReportPdfFilters {
  classId?: string;
  studentId?: string;
  dateFrom?: string;
  dateTo?: string;
}

export const reportsService = {
  async downloadPdf(type: ReportPdfType, filters: ReportPdfFilters) {
    const response = await apiClient.get<Blob>(`/reports/${type}/pdf`, {
      params: filters,
      responseType: "blob"
    });

    return {
      blob: response.data,
      fileName: getFileName(response.headers["content-disposition"], `${type}-report.pdf`)
    };
  }
};

function getFileName(contentDisposition: string | undefined, fallback: string) {
  if (!contentDisposition) {
    return fallback;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const quotedMatch = contentDisposition.match(/filename="([^"]+)"/i);
  if (quotedMatch?.[1]) {
    return quotedMatch[1];
  }

  const plainMatch = contentDisposition.match(/filename=([^;]+)/i);
  if (plainMatch?.[1]) {
    return plainMatch[1].trim();
  }

  return fallback;
}
