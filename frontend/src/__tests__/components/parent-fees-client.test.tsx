import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { ParentFeesClient } from "@/features/parent-portal/components/parent-fees-client";

// ── auth ──────────────────────────────────────────────────────────────────────
vi.mock("@/store/auth.store", () => ({
  useAuthStore: () => ({ user: { id: "u1" } })
}));

// ── children query ────────────────────────────────────────────────────────────
let childrenState = {
  isLoading: false,
  isError: false,
  data: {
    items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }]
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

// ── fees query ────────────────────────────────────────────────────────────────
let feesState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      {
        id: "f1",
        feeType: "Tuition",
        amount: 500,
        dueDate: "2024-11-01",
        status: "Pending" as const,
        payments: []
      },
      {
        id: "f2",
        feeType: "Library",
        amount: 50,
        dueDate: "2024-10-15",
        status: "Paid" as const,
        payments: [{ paymentDate: "2024-10-10", amount: 50 }]
      }
    ]
  }
};

vi.mock("@/features/parent-portal/hooks/use-parent-portal", () => ({
  useChildFees: () => feesState,
  useChildAttendance: vi.fn(),
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
  DataTable: ({ rows }: { rows: { id: string; feeType: string; status: string; amount: number }[] }) => (
    <div data-testid="data-table">
      {rows.map((f) => (
        <div key={f.id} data-testid={`row-${f.id}`}>
          <span>{f.feeType}</span>
          <span>{f.status}</span>
          <span>${f.amount.toFixed(2)}</span>
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

vi.mock("@/components/ui/select", () => ({
  Select: ({
    children,
    placeholder,
    onChange,
    value
  }: React.SelectHTMLAttributes<HTMLSelectElement> & { placeholder?: string }) => (
    <select data-testid="status-filter" value={value} onChange={onChange}>
      {placeholder && <option value="">{placeholder}</option>}
      {children}
    </select>
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

// ── Tests ─────────────────────────────────────────────────────────────────────
describe("ParentFeesClient — guard states", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = { isLoading: false, isError: false, data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] } };
    feesState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "f1", feeType: "Tuition", amount: 500, dueDate: "2024-11-01", status: "Pending", payments: [] },
          { id: "f2", feeType: "Library", amount: 50, dueDate: "2024-10-15", status: "Paid", payments: [{ paymentDate: "2024-10-10", amount: 50 }] }
        ]
      }
    };
  });

  it("shows loading state while children query is pending", () => {
    childrenState = { ...childrenState, isLoading: true, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows error state when children query fails", () => {
    childrenState = { ...childrenState, isError: true, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("Unable to load children");
  });

  it("shows no-child state when parent has no linked students", () => {
    childrenState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("No child linked");
  });
});

describe("ParentFeesClient — fee data display", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = { isLoading: false, isError: false, data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] } };
    feesState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "f1", feeType: "Tuition", amount: 500, dueDate: "2024-11-01", status: "Pending", payments: [] },
          { id: "f2", feeType: "Library", amount: 50, dueDate: "2024-10-15", status: "Paid", payments: [{ paymentDate: "2024-10-10", amount: 50 }] }
        ]
      }
    };
  });

  it("renders the page title", () => {
    render(<ParentFeesClient />);
    expect(screen.getByTestId("page-title")).toHaveTextContent("Fees");
  });

  it("includes active child's name in description", () => {
    render(<ParentFeesClient />);
    expect(screen.getByTestId("page-description")).toHaveTextContent("Alice Child");
  });

  it("renders both fee rows in the data table", () => {
    render(<ParentFeesClient />);
    expect(screen.getByTestId("row-f1")).toBeInTheDocument();
    expect(screen.getByTestId("row-f2")).toBeInTheDocument();
  });

  it("renders summary stat cards", () => {
    render(<ParentFeesClient />);
    const cards = screen.getAllByTestId("summary-card");
    expect(cards.length).toBe(3); // total, paid, outstanding
  });

  it("shows fees loading state while query is pending", () => {
    feesState = { ...feesState, isLoading: true, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows fees error state when query fails", () => {
    feesState = { ...feesState, isError: true, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("Unable to load fees");
  });

  it("shows empty state when no fee records exist", () => {
    feesState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("empty-state")).toHaveTextContent("No fees found");
  });
});

describe("ParentFeesClient — status filter", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    childrenState = { isLoading: false, isError: false, data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] } };
    feesState = {
      isLoading: false,
      isError: false,
      data: {
        items: [
          { id: "f1", feeType: "Tuition", amount: 500, dueDate: "2024-11-01", status: "Pending", payments: [] },
          { id: "f2", feeType: "Library", amount: 50, dueDate: "2024-10-15", status: "Paid", payments: [] }
        ]
      }
    };
  });

  it("renders the status filter select", () => {
    render(<ParentFeesClient />);
    expect(screen.getByTestId("status-filter")).toBeInTheDocument();
  });

  it("filters to show only Pending fees when Pending is selected", () => {
    render(<ParentFeesClient />);
    fireEvent.change(screen.getByTestId("status-filter"), { target: { value: "Pending" } });
    expect(screen.getByTestId("row-f1")).toBeInTheDocument();
    expect(screen.queryByTestId("row-f2")).toBeNull();
  });

  it("filters to show only Paid fees when Paid is selected", () => {
    render(<ParentFeesClient />);
    fireEvent.change(screen.getByTestId("status-filter"), { target: { value: "Paid" } });
    expect(screen.queryByTestId("row-f1")).toBeNull();
    expect(screen.getByTestId("row-f2")).toBeInTheDocument();
  });

  it("shows Clear button when a status filter is active", () => {
    render(<ParentFeesClient />);
    fireEvent.change(screen.getByTestId("status-filter"), { target: { value: "Pending" } });
    expect(screen.getByText("Clear")).toBeInTheDocument();
  });

  it("restores all fees when Clear is clicked", () => {
    render(<ParentFeesClient />);
    fireEvent.change(screen.getByTestId("status-filter"), { target: { value: "Pending" } });
    fireEvent.click(screen.getByText("Clear"));
    expect(screen.getByTestId("row-f1")).toBeInTheDocument();
    expect(screen.getByTestId("row-f2")).toBeInTheDocument();
  });

  it("does not show Clear button when no filter is active", () => {
    render(<ParentFeesClient />);
    expect(screen.queryByText("Clear")).toBeNull();
  });
});

describe("ParentFeesClient — child switcher", () => {
  beforeEach(() => vi.clearAllMocks());

  it("renders child switcher", () => {
    childrenState = { isLoading: false, isError: false, data: { items: [{ id: "s1", fullName: "Alice Child", className: "Grade 7" }] } };
    feesState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentFeesClient />);
    expect(screen.getByTestId("child-switcher")).toBeInTheDocument();
  });

  it("calls setSelectedChildId when child is changed", () => {
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
    feesState = { isLoading: false, isError: false, data: { items: [] } };
    render(<ParentFeesClient />);
    fireEvent.change(screen.getByTestId("child-switcher"), { target: { value: "s2" } });
    expect(mockSetSelectedChildId).toHaveBeenCalledWith("s2");
  });
});
