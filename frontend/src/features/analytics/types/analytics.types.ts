// ---------------------------------------------------------------------------
// KPI snapshot
// ---------------------------------------------------------------------------

export interface KpiResponse {
  totalStudents: number;
  totalTeachers: number;
  totalClasses: number;
  attendanceRate: number;
  presentCount: number;
  absentCount: number;
  lateCount: number;
  excusedCount: number;
  unpaidFeesCount: number;
  totalCollectedPayments: number;
  recentNotificationsCount: number;
  examPassRate: number;
  examAverageScore: number;
}

// ---------------------------------------------------------------------------
// Attendance trends
// ---------------------------------------------------------------------------

export interface AttendanceTrendPoint {
  date: string;
  present: number;
  absent: number;
  late: number;
  excused: number;
}

export interface AttendanceTrendsResponse {
  trends: AttendanceTrendPoint[];
  daysRequested: number;
}

// ---------------------------------------------------------------------------
// Exam performance
// ---------------------------------------------------------------------------

export interface ExamPerformanceItem {
  examTitle: string;
  subjectName: string;
  className: string;
  averageScore: number;
  totalMarks: number;
  passCount: number;
  failCount: number;
  totalSubmissions: number;
}

export interface ExamPerformanceResponse {
  examAverages: ExamPerformanceItem[];
  overallPassRate: number;
  overallAverageScore: number;
  totalExamsWithResults: number;
}

// ---------------------------------------------------------------------------
// Finance summary
// ---------------------------------------------------------------------------

export interface FinanceSummaryResponse {
  totalFeesAmount: number;
  paidAmount: number;
  pendingAmount: number;
  overdueAmount: number;
  partiallyPaidAmount: number;
  paidCount: number;
  pendingCount: number;
  overdueCount: number;
  partiallyPaidCount: number;
  totalCollectedPayments: number;
  totalPaymentsCount: number;
}

// ---------------------------------------------------------------------------
// Filter params
// ---------------------------------------------------------------------------

export interface AttendanceTrendsParams {
  days?: number;
}

export interface ExamPerformanceParams {
  classId?: string;
}
