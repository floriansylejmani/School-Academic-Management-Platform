import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { AdminDashboardClient } from "@/features/dashboard/admin-dashboard-client";

const mockKpisRefetch = vi.fn();
const mockTrendRefetch = vi.fn();
const mockExamRefetch = vi.fn();
const mockFinanceRefetch = vi.fn();
const mockClassesRefetch = vi.fn();

let kpisState = {
  isLoading: false,
  isError: false,
  refetch: mockKpisRefetch,
  data: {
    totalStudents: 420,
    totalTeachers: 32,
    totalClasses: 18,
    attendanceRate: 91.2,
    presentCount: 385,
    lateCount: 14,
    absentCount: 21,
    examPassRate: 84.4,
    examAverageScore: 78.3,
    unpaidFeesCount: 9,
    totalCollectedPayments: 128500,
    recentNotificationsCount: 17
  }
};

let attendanceTrendState = {
  isLoading: false,
  isError: false,
  refetch: mockTrendRefetch,
  data: {
    trends: [
      { date: "2026-04-10", present: 120, absent: 4, late: 2, excused: 1 }
    ]
  }
};

let examPerformanceState = {
  isLoading: false,
  isError: false,
  refetch: mockExamRefetch,
  data: {
    overallPassRate: 82.1,
    overallAverageScore: 76.4,
    totalExamsWithResults: 4,
    examAverages: [
      {
        examTitle: "Midterm Mathematics",
        subjectName: "Mathematics",
        className: "Grade 10 A",
        averageScore: 76,
        totalMarks: 100,
        passCount: 22,
        failCount: 3
      }
    ]
  }
};

let financeSummaryState = {
  isLoading: false,
  isError: false,
  refetch: mockFinanceRefetch,
  data: {
    paidCount: 12,
    pendingCount: 4,
    overdueCount: 2,
    partiallyPaidCount: 1,
    paidAmount: 98000,
    pendingAmount: 18000,
    overdueAmount: 9000,
    partiallyPaidAmount: 3500,
    totalCollectedPayments: 128500,
    totalFeesAmount: 159500
  }
};

let classesState = {
  isLoading: false,
  isError: false,
  refetch: mockClassesRefetch,
  data: {
    items: [
      { id: "class-1", name: "Grade 10", section: "A" }
    ],
    totalCount: 1
  }
};

vi.mock("@/features/analytics/hooks/use-analytics", () => ({
  useAnalyticsKpis: () => kpisState,
  useAttendanceTrends: () => attendanceTrendState,
  useExamPerformance: () => examPerformanceState,
  useFinanceSummary: () => financeSummaryState
}));

vi.mock("@/features/classes/hooks/use-classes", () => ({
  useClasses: () => classesState
}));

vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ title, description }: { title: string; description?: string }) => (
    <div>
      <h1 data-testid="page-title">{title}</h1>
      {description ? <p data-testid="page-description">{description}</p> : null}
    </div>
  )
}));

vi.mock("@/components/ui/empty-state", () => ({
  EmptyState: ({
    title,
    action
  }: {
    title: string;
    action?: React.ReactNode;
  }) => (
    <div data-testid="empty-state">
      <span data-testid="empty-title">{title}</span>
      {action}
    </div>
  )
}));

vi.mock("@/components/ui/card", () => ({
  Card: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

vi.mock("@/components/ui/button", () => ({
  Button: ({
    children,
    onClick
  }: {
    children: React.ReactNode;
    onClick?: () => void;
  }) => <button onClick={onClick}>{children}</button>
}));

vi.mock("@/components/ui/select", () => ({
  Select: ({ children }: { children: React.ReactNode }) => <select>{children}</select>
}));

vi.mock("@/features/analytics/components/kpi-card", () => ({
  KpiCard: ({ label, value }: { label: string; value: string }) => (
    <div data-testid="kpi-card">
      <span>{label}</span>
      <span>{value}</span>
    </div>
  )
}));

vi.mock("@/features/analytics/components/kpi-skeleton", () => ({
  KpiSkeleton: () => <div data-testid="kpi-skeleton">Loading KPI</div>,
  ChartSkeleton: () => <div data-testid="chart-skeleton">Loading chart</div>
}));

vi.mock("@/features/analytics/components/attendance-trend-chart", () => ({
  AttendanceTrendChart: ({ trends }: { trends: Array<{ date: string }> }) => (
    <div data-testid="attendance-chart">{trends.length} attendance points</div>
  )
}));

vi.mock("@/features/analytics/components/exam-performance-chart", () => ({
  ExamPerformanceChart: ({ examAverages }: { examAverages: Array<{ examTitle: string }> }) => (
    <div data-testid="exam-chart">{examAverages.length} exam averages</div>
  )
}));

vi.mock("@/features/analytics/components/finance-summary-chart", () => ({
  FinanceSummaryChart: ({ data }: { data: { totalFeesAmount: number } }) => (
    <div data-testid="finance-chart">{data.totalFeesAmount}</div>
  )
}));

vi.mock("lucide-react", () => ({
  Banknote: () => <span />,
  Bell: () => <span />,
  BookOpenCheck: () => <span />,
  ClipboardCheck: () => <span />,
  GraduationCap: () => <span />,
  ReceiptText: () => <span />,
  RefreshCw: () => <span />,
  School: () => <span />,
  Users: () => <span />,
  UserPlus: () => <span />,
  MessageSquare: () => <span />,
  FileText: () => <span />,
  Plus: () => <span />,
  ChevronDown: () => <span />,
  TrendingUp: () => <span />
}));

describe("AdminDashboardClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    kpisState = {
      isLoading: false,
      isError: false,
      refetch: mockKpisRefetch,
      data: {
        totalStudents: 420,
        totalTeachers: 32,
        totalClasses: 18,
        attendanceRate: 91.2,
        presentCount: 385,
        lateCount: 14,
        absentCount: 21,
        examPassRate: 84.4,
        examAverageScore: 78.3,
        unpaidFeesCount: 9,
        totalCollectedPayments: 128500,
        recentNotificationsCount: 17
      }
    };

    attendanceTrendState = {
      isLoading: false,
      isError: false,
      refetch: mockTrendRefetch,
      data: {
        trends: [
          { date: "2026-04-10", present: 120, absent: 4, late: 2, excused: 1 }
        ]
      }
    };

    examPerformanceState = {
      isLoading: false,
      isError: false,
      refetch: mockExamRefetch,
      data: {
        overallPassRate: 82.1,
        overallAverageScore: 76.4,
        totalExamsWithResults: 4,
        examAverages: [
          {
            examTitle: "Midterm Mathematics",
            subjectName: "Mathematics",
            className: "Grade 10 A",
            averageScore: 76,
            totalMarks: 100,
            passCount: 22,
            failCount: 3
          }
        ]
      }
    };

    financeSummaryState = {
      isLoading: false,
      isError: false,
      refetch: mockFinanceRefetch,
      data: {
        paidCount: 12,
        pendingCount: 4,
        overdueCount: 2,
        partiallyPaidCount: 1,
        paidAmount: 98000,
        pendingAmount: 18000,
        overdueAmount: 9000,
        partiallyPaidAmount: 3500,
        totalCollectedPayments: 128500,
        totalFeesAmount: 159500
      }
    };

    classesState = {
      isLoading: false,
      isError: false,
      refetch: mockClassesRefetch,
      data: {
        items: [
          { id: "class-1", name: "Grade 10", section: "A" }
        ],
        totalCount: 1
      }
    };
  });

  it("renders the Good afternoon, Admin ?? header and KPI cards", () => {
    render(<AdminDashboardClient />);

    expect(screen.getByTestId("page-title")).toHaveTextContent("Good afternoon, Admin");
    expect(screen.getByText("Total Students")).toBeInTheDocument();
    expect(screen.getByText("420")).toBeInTheDocument();
    expect(screen.getAllByText("$128,500")).toHaveLength(2);
  });

  it("renders analytics chart sections with current query data", () => {
    render(<AdminDashboardClient />);

    expect(screen.getByText("Key Performance Indicators")).toBeInTheDocument();
    expect(screen.getByTestId("attendance-chart")).toHaveTextContent("1 attendance points");
    expect(screen.getByTestId("exam-chart")).toHaveTextContent("1 exam averages");
    expect(screen.getByTestId("finance-chart")).toHaveTextContent("159500");
  });

  it("shows skeletons while analytics queries are loading", () => {
    kpisState = { ...kpisState, isLoading: true };
    attendanceTrendState = { ...attendanceTrendState, isLoading: true };
    examPerformanceState = { ...examPerformanceState, isLoading: true };
    financeSummaryState = { ...financeSummaryState, isLoading: true };

    render(<AdminDashboardClient />);

    expect(screen.getAllByTestId("kpi-skeleton")).toHaveLength(8);
    expect(screen.getAllByTestId("chart-skeleton")).toHaveLength(3);
  });

  it("shows an error state when an analytics query fails", () => {
    financeSummaryState = { ...financeSummaryState, isError: true };

    render(<AdminDashboardClient />);

    expect(screen.getByTestId("empty-state")).toBeInTheDocument();
    expect(screen.getByTestId("empty-title")).toHaveTextContent("Unable to load dashboard data");
  });

  it("refetches every analytics query when Retry is clicked from the error state", () => {
    kpisState = { ...kpisState, isError: true };

    render(<AdminDashboardClient />);
    fireEvent.click(screen.getByText("Retry Loading"));

    expect(mockKpisRefetch).toHaveBeenCalledTimes(1);
    expect(mockTrendRefetch).toHaveBeenCalledTimes(1);
    expect(mockExamRefetch).toHaveBeenCalledTimes(1);
    expect(mockFinanceRefetch).toHaveBeenCalledTimes(1);
  });
});
