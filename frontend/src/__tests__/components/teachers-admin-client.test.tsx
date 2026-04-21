import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { TeachersAdminClient } from "@/features/teachers/components/teachers-admin-client";

// ── mock all four hooks the component depends on ───────────────────────────────
const mockMutateAsync = vi.fn();

vi.mock("@/features/teachers/hooks/use-teachers", () => ({
  useTeachers: () => ({
    isLoading: false,
    isError: false,
    data: {
      items: [
        {
          id: "t1",
          fullName: "Alice Smith",
          email: "alice@school.com",
          teacherCode: "TCH001",
          specialization: "Math",
          hireDate: "2022-01-01"
        }
      ]
    },
    refetch: vi.fn()
  }),
  useCreateTeacher: () => ({
    mutateAsync: mockMutateAsync,
    isPending: false
  }),
  useUpdateTeacher: () => ({
    mutateAsync: mockMutateAsync,
    isPending: false
  }),
  useDeleteTeacher: () => ({
    mutateAsync: mockMutateAsync,
    isPending: false
  })
}));

// ── mock TeacherForm so we control what it renders and submits ─────────────────
vi.mock("@/features/teachers/components/teacher-form", () => ({
  TeacherForm: ({ onSubmit }: { onSubmit: (payload: unknown) => void }) => (
    <button
      data-testid="submit-form"
      onClick={() => onSubmit({ fullName: "Test Teacher", email: "t@t.com" })}
    >
      Submit
    </button>
  )
}));

// ── stub UI primitives so they render minimally ────────────────────────────────
vi.mock("@/components/ui/modal", () => ({
  Modal: ({
    open,
    children,
    title
  }: {
    open: boolean;
    children: React.ReactNode;
    title: string;
  }) =>
    open ? (
      <div data-testid="modal" data-title={title}>
        {children}
      </div>
    ) : null
}));

vi.mock("@/components/ui/confirm-delete-dialog", () => ({
  ConfirmDeleteDialog: ({
    open,
    onConfirm,
    onCancel
  }: {
    open: boolean;
    onConfirm: () => void;
    onCancel: () => void;
  }) =>
    open ? (
      <div data-testid="confirm-dialog">
        <button data-testid="confirm-delete" onClick={onConfirm}>
          Confirm
        </button>
        <button data-testid="cancel-delete" onClick={onCancel}>
          Cancel
        </button>
      </div>
    ) : null
}));

vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ actionLabel, onAction }: { actionLabel: string; onAction: () => void }) => (
    <button data-testid="page-action" onClick={onAction}>
      {actionLabel}
    </button>
  )
}));

vi.mock("@/components/ui/data-table", () => ({
  DataTable: ({
    rows,
    onEdit,
    onDelete
  }: {
    rows: { id: string; fullName: string }[];
    onEdit: (row: unknown) => void;
    onDelete: (row: unknown) => void;
  }) => (
    <div data-testid="data-table">
      {rows.map((row) => (
        <div key={row.id}>
          <span>{row.fullName}</span>
          <button data-testid={`edit-${row.id}`} onClick={() => onEdit(row)}>
            Edit
          </button>
          <button data-testid={`delete-${row.id}`} onClick={() => onDelete(row)}>
            Delete
          </button>
        </div>
      ))}
    </div>
  )
}));

vi.mock("@/components/ui/badge", () => ({
  Badge: ({ children }: { children: React.ReactNode }) => <span>{children}</span>
}));

vi.mock("@/components/ui/button", () => ({
  Button: ({ children, onClick }: { children: React.ReactNode; onClick?: () => void }) => (
    <button onClick={onClick}>{children}</button>
  )
}));

// ── tests ──────────────────────────────────────────────────────────────────────
describe("TeachersAdminClient — create modal", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("opens create modal when page action is clicked", () => {
    render(<TeachersAdminClient />);
    expect(screen.queryByTestId("modal")).toBeNull();

    fireEvent.click(screen.getByTestId("page-action"));

    expect(screen.getByTestId("modal")).toBeInTheDocument();
  });

  it("closes create modal after a successful submission", async () => {
    mockMutateAsync.mockResolvedValueOnce({ id: "t2", fullName: "Test Teacher" });

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("page-action"));
    expect(screen.getByTestId("modal")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("submit-form"));

    await waitFor(() => {
      expect(screen.queryByTestId("modal")).toBeNull();
    });
  });

  it("keeps create modal open after a failed submission", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("A user with this email already exists."));

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("page-action"));
    expect(screen.getByTestId("modal")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("submit-form"));

    await waitFor(() => {
      expect(screen.getByTestId("modal")).toBeInTheDocument();
    });
  });
});

describe("TeachersAdminClient — edit modal", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("opens edit modal when edit button is clicked", () => {
    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("edit-t1"));
    expect(screen.getByTestId("modal")).toBeInTheDocument();
  });

  it("closes edit modal after a successful update", async () => {
    mockMutateAsync.mockResolvedValueOnce({ id: "t1", fullName: "Updated" });

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("edit-t1"));
    fireEvent.click(screen.getByTestId("submit-form"));

    await waitFor(() => {
      expect(screen.queryByTestId("modal")).toBeNull();
    });
  });

  it("keeps edit modal open after a failed update", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Update failed."));

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("edit-t1"));
    fireEvent.click(screen.getByTestId("submit-form"));

    await waitFor(() => {
      expect(screen.getByTestId("modal")).toBeInTheDocument();
    });
  });
});

describe("TeachersAdminClient — delete confirmation", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows confirm dialog when delete is clicked", () => {
    render(<TeachersAdminClient />);
    expect(screen.queryByTestId("confirm-dialog")).toBeNull();

    fireEvent.click(screen.getByTestId("delete-t1"));

    expect(screen.getByTestId("confirm-dialog")).toBeInTheDocument();
  });

  it("closes confirm dialog after successful delete", async () => {
    mockMutateAsync.mockResolvedValueOnce(undefined);

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("delete-t1"));
    fireEvent.click(screen.getByTestId("confirm-delete"));

    await waitFor(() => {
      expect(screen.queryByTestId("confirm-dialog")).toBeNull();
    });
  });

  it("keeps confirm dialog open after a failed delete", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Delete failed."));

    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("delete-t1"));
    fireEvent.click(screen.getByTestId("confirm-delete"));

    await waitFor(() => {
      expect(screen.getByTestId("confirm-dialog")).toBeInTheDocument();
    });
  });

  it("closes confirm dialog when cancel is clicked", () => {
    render(<TeachersAdminClient />);
    fireEvent.click(screen.getByTestId("delete-t1"));
    fireEvent.click(screen.getByTestId("cancel-delete"));

    expect(screen.queryByTestId("confirm-dialog")).toBeNull();
  });
});
