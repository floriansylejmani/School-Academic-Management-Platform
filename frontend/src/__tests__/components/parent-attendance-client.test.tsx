import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { ParentAttendanceClient } from "@/features/parent-portal/components/parent-attendance-client";

// ── auth ──────────────────────────────────────────────────────────────────────
vi.mock("@/store/auth.store", () => ({
  useAuthStore: () => ({ user: { id: "u1" } })
}));

// ── children query ────────────────────────────────────────────────────────────
let childrenState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      { id: "s1", fullName: "Alice Child", className: "Grade 7" }
    ]
  }
};

vi.mock("@/features/profile/hooks/use-profile", () => ({
  useParentChildren: () => childrenState
}));

// ── child-selection hook ──────────────────────────────────────────────────────
const mockSetSelectedChildId = vi.fn();

vi.mock("@/features/parent-portal/hooks/use-parent-child-selection", () => ({
  useParentChildSelection: (children: { id: string; fullName: string }[]) => ({
    activeChild: children[0] ?? null,
    activeChildId: children[0]?.id,
    setSelectedChildId: mockSetSelectedChildId
  })
}));

// ── attendance query ──────────────────────────────────────────────────────────
let attendanceState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      {
        id: "a1",
        date: "2024-10-01",
        subjectName: "Mathematics",
        teacherName: "Mr. Smith",
        status: "Present" as const,
        remarks: null
      },
      {
        id: "a2",
        date: "2024-10-02",
        subjectName: "Biology",
        teacherName: "Ms. Jones",
        status: "Absent" as const,
        remarks: "Sick"
      }
    ]
  }
};

vi.mock("@/features/parent-portal/hooks/use-parent-portal", () => ({
  useChildAttendance: () => attendanceState,
  useChildFees: vi.fn(),
  useChildResults: vi.fn(),
  useChildExams: vi.fn(),
  useChildTimetable: vi.fn(),
  useParentDashboardOverview: vi.fn()
}));

// ── UI stubs ──────────────────────────────────────────────────────────────────
vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ title, description }: { title: string; description: string }) => (
    <div>
      <h1 data-testid="page-title">{title}</h1>
      <p data-testid="page-description">{description}</p>
    </div>
  )
}));

vi.mock("@/components/ui/card", () => ({
  Card: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="summary-card">{children}</div>
  )
}));

vi.mock("@/components/ui/data-table", () => ({
  DataTable: ({ rows }: { rows: { id: string; date: string; subjectName: string; status: string }[] }) => (
    <div data-testid="data-table">
      {rows.map((r) => (
        <div key={r.id} data-testid={`row-${r.id}`}>
          <span>{r.date}</span>
          <span>{r.subjectName}</span>
          <span>{r.status}</span>
        </div>
      ))}
    </div>
  )
}));

vi.mock("@/components/ui/empty-state", () => ({
  EmptyState: ({ title }: { title: string }) => (
    <div data-testid="empty-state">{title}</div>
  )
}));

vi.mock("@/components/ui/loading-state", () => ({
  LoadingState: ({ title }: { title: string }) => (
    <div data-testid="loading-state">{title}</div>
  )
}));

vi.mock("@/components/ui/input", () => ({
  Input: (props: React.InputHTMLAttributes<HTMLInputElement>) => (
    <input {...props} />
  )
}));

vi.mock("@/features/parent-portal/components/parent-child-switcher", () => ({
  ParentChildSwitcher: ({
    students,
    onChange
  }: {
    students: { id: string; fullName: string }[];
    value: string | undefined;
    onChange: (id: string) => void;
  }) => (
    <select
      data-testid="child-switcher"
      onChange={(e) => onChange(e.target.value)}
    >
      {students.map((c) => (
        <option key={c.id} value={c.id}>
          {c.fullName}
        </option>
      ))}
    </select>
  )
}));

// ── tests ─────────────────────────────────────────────────────────────────────
describe("ParentAttendanceClient — guard states", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = {
      isLoading: false,
      isError: false,
      data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] }
    };
    attendanceState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "a1", date: "2024-10-01", subjectName: "Mathematics", teacherName: "Mr. Smith", status: "Present", remarks: null },
          { id: "a2", date: "2024-10-02", subjectName: "Biology", teacherName: "Ms. Jones", status: "Absent", remarks: "Sick" }
        ]
      }
    };
  });

  it("shows loading state while children query is pending", () => {
    childrenState = { ...childrenState, isLoading: true, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows error state when children query fails", () => {
    childrenState = { ...childrenState, isError: true, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("Unable to load children");
  });

  it("shows no-child state when parent has no linked students", () => {
    childrenState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("No child linked");
  });
});

describe("ParentAttendanceClient — attendance data display", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = {
      isLoading: false,
      isError: false,
      data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] }
    };
    attendanceState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "a1", date: "2024-10-01", subjectName: "Mathematics", teacherName: "Mr. Smith", status: "Present", remarks: null },
          { id: "a2", date: "2024-10-02", subjectName: "Biology", teacherName: "Ms. Jones", status: "Absent", remarks: "Sick" }
        ]
      }
    };
  });

  it("renders the page title", () => {
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("page-title")).toHaveTextContent("Attendance Record");
  });

  it("includes active child's name in description", () => {
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("page-description")).toHaveTextContent("Alice Child");
  });

  it("renders the data table with attendance rows", () => {
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("data-table")).toBeInTheDocument();
    expect(screen.getByTestId("row-a1")).toBeInTheDocument();
    expect(screen.getByTestId("row-a2")).toBeInTheDocument();
  });

  it("shows attendance loading state while query is pending", () => {
    attendanceState = { ...attendanceState, isLoading: true, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows attendance error state when query fails", () => {
    attendanceState = { ...attendanceState, isError: true, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("Unable to load attendance");
  });

  it("shows empty state when no attendance records exist", () => {
    attendanceState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("No attendance records");
  });

  it("renders summary stat cards", () => {
    render(<ParentAttendanceClient />);
    const cards = screen.getAllByTestId("summary-card");
    expect(cards.length).toBeGreaterThanOrEqual(4);
  });
});

describe("ParentAttendanceClient — date filter", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = {
      isLoading: false,
      isError: false,
      data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] }
    };
    attendanceState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "a1", date: "2024-10-01", subjectName: "Mathematics", teacherName: "Mr. Smith", status: "Present", remarks: null },
          { id: "a2", date: "2024-10-03", subjectName: "Biology", teacherName: "Ms. Jones", status: "Absent", remarks: "Sick" }
        ]
      }
    };
  });

  it("renders date from and to inputs", () => {
    render(<ParentAttendanceClient />);
    const dateInputs = screen.getAllByDisplayValue("");
    // Both date inputs start empty
    expect(dateInputs.length).toBeGreaterThanOrEqual(2);
  });

  it("filters out records before the From date", () => {
    render(<ParentAttendanceClient />);
    const [fromInput] = screen.getAllByDisplayValue("");
    fireEvent.change(fromInput, { target: { value: "2024-10-02" } });
    // a1 (2024-10-01) should be filtered out
    expect(screen.queryByTestId("row-a1")).toBeNull();
    expect(screen.getByTestId("row-a2")).toBeInTheDocument();
  });

  it("shows Clear button when a date filter is active", () => {
    render(<ParentAttendanceClient />);
    const [fromInput] = screen.getAllByDisplayValue("");
    fireEvent.change(fromInput, { target: { value: "2024-10-02" } });
    expect(screen.getByText("Clear")).toBeInTheDocument();
  });

  it("clears date filters when Clear is clicked", () => {
    render(<ParentAttendanceClient />);
    const [fromInput] = screen.getAllByDisplayValue("");
    fireEvent.change(fromInput, { target: { value: "2024-10-02" } });
    fireEvent.click(screen.getByText("Clear"));
    // Both records should be visible again
    expect(screen.getByTestId("row-a1")).toBeInTheDocument();
    expect(screen.getByTestId("row-a2")).toBeInTheDocument();
  });
});

describe("ParentAttendanceClient — child switcher", () => {
  beforeEach(() => vi.clearAllMocks());

  it("renders the child switcher", () => {
    childrenState = {
      isLoading: false,
      isError: false,
      data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] }
    };
    attendanceState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentAttendanceClient />);
    expect(screen.getByTestId("child-switcher")).toBeInTheDocument();
  });

  it("calls setSelectedChildId when a different child is selected", () => {
    childrenState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "s1", fullName: "Alice Child", className: "Grade 7" },
          { id: "s2", fullName: "Bob Child", className: "Grade 5" }
        ]
      }
    };
    attendanceState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentAttendanceClient />);
    fireEvent.change(screen.getByTestId("child-switcher"), { target: { value: "s2" } });
    expect(mockSetSelectedChildId).toHaveBeenCalledWith("s2");
  });
});
