import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { ParentsAdminClient } from "@/features/parents/components/parents-admin-client";

const mockMutateAsync = vi.fn();

vi.mock("@/features/parents/hooks/use-parents", () => ({
  useParents: () => ({
    isLoading: false,
    isError: false,
    data: {
      items: [
        {
          id: "p1",
          userId: "u10",
          fullName: "Carol Parent",
          email: "carol@school.com",
          phone: "555-0100",
          address: "1 Main St",
          occupation: "Accountant",
          studentsCount: 2,
          createdAt: "2024-09-01T00:00:00Z"
        }
      ]
    },
    refetch: vi.fn()
  }),
  useCreateParent: () => ({ mutateAsync: mockMutateAsync, isPending: false }),
  useUpdateParent: () => ({ mutateAsync: mockMutateAsync, isPending: false }),
  useDeleteParent: () => ({ mutateAsync: mockMutateAsync, isPending: false })
}));

vi.mock("@/features/parents/components/parent-form", () => ({
  ParentForm: ({ onSubmit }: { onSubmit: (p: unknown) => void }) => (
    <button data-testid="submit-form" onClick={() => onSubmit({ fullName: "Carol Parent" })}>
      Submit
    </button>
  )
}));

vi.mock("@/components/ui/modal", () => ({
  Modal: ({ open, children, title }: { open: boolean; children: React.ReactNode; title: string }) =>
    open ? <div data-testid="modal" data-title={title}>{children}</div> : null
}));

vi.mock("@/components/ui/confirm-delete-dialog", () => ({
  ConfirmDeleteDialog: ({
    open, onConfirm, onCancel
  }: { open: boolean; onConfirm: () => void; onCancel: () => void }) =>
    open ? (
      <div data-testid="confirm-dialog">
        <button data-testid="confirm-delete" onClick={onConfirm}>Confirm</button>
        <button data-testid="cancel-delete" onClick={onCancel}>Cancel</button>
      </div>
    ) : null
}));

vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ actionLabel, onAction }: { actionLabel?: string; onAction?: () => void }) => (
    <button data-testid="page-action" onClick={onAction}>{actionLabel}</button>
  )
}));

vi.mock("@/components/ui/data-table", () => ({
  DataTable: ({
    rows, onEdit, onDelete
  }: { rows: { id: string; fullName: string }[]; onEdit: (r: unknown) => void; onDelete: (r: unknown) => void }) => (
    <div data-testid="data-table">
      {rows.map((row) => (
        <div key={row.id}>
          <span>{row.fullName}</span>
          <button data-testid={`edit-${row.id}`} onClick={() => onEdit(row)}>Edit</button>
          <button data-testid={`delete-${row.id}`} onClick={() => onDelete(row)}>Delete</button>
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

vi.mock("@/components/ui/empty-state", () => ({
  EmptyState: ({ title }: { title: string }) => <div data-testid="empty-state">{title}</div>
}));

vi.mock("@/components/ui/loading-state", () => ({
  LoadingState: ({ title }: { title: string }) => <div data-testid="loading-state">{title}</div>
}));

describe("ParentsAdminClient — create modal", () => {
  beforeEach(() => vi.clearAllMocks());

  it("is hidden on initial render", () => {
    render(<ParentsAdminClient />);
    expect(screen.queryByTestId("modal")).toBeNull();
  });

  it("opens with correct title on page action click", () => {
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("page-action"));
    expect(screen.getByTestId("modal")).toHaveAttribute("data-title", "Create parent");
  });

  it("closes after a successful create", async () => {
    mockMutateAsync.mockResolvedValueOnce({ id: "p2", fullName: "New Parent" });
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("page-action"));
    fireEvent.click(screen.getByTestId("submit-form"));
    await waitFor(() => expect(screen.queryByTestId("modal")).toBeNull());
  });

  it("stays open after a failed create", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Email taken."));
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("page-action"));
    fireEvent.click(screen.getByTestId("submit-form"));
    await waitFor(() => expect(screen.getByTestId("modal")).toBeInTheDocument());
  });
});

describe("ParentsAdminClient — edit modal", () => {
  beforeEach(() => vi.clearAllMocks());

  it("opens with correct title on edit click", () => {
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("edit-p1"));
    expect(screen.getByTestId("modal")).toHaveAttribute("data-title", "Edit parent");
  });

  it("closes after a successful update", async () => {
    mockMutateAsync.mockResolvedValueOnce({ id: "p1", fullName: "Updated" });
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("edit-p1"));
    fireEvent.click(screen.getByTestId("submit-form"));
    await waitFor(() => expect(screen.queryByTestId("modal")).toBeNull());
  });

  it("stays open after a failed update", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Update error."));
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("edit-p1"));
    fireEvent.click(screen.getByTestId("submit-form"));
    await waitFor(() => expect(screen.getByTestId("modal")).toBeInTheDocument());
  });
});

describe("ParentsAdminClient — delete dialog", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows confirm dialog on delete click", () => {
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("delete-p1"));
    expect(screen.getByTestId("confirm-dialog")).toBeInTheDocument();
  });

  it("closes after successful deletion", async () => {
    mockMutateAsync.mockResolvedValueOnce(undefined);
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("delete-p1"));
    fireEvent.click(screen.getByTestId("confirm-delete"));
    await waitFor(() => expect(screen.queryByTestId("confirm-dialog")).toBeNull());
  });

  it("stays open after a failed deletion", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Has linked students."));
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("delete-p1"));
    fireEvent.click(screen.getByTestId("confirm-delete"));
    await waitFor(() => expect(screen.getByTestId("confirm-dialog")).toBeInTheDocument());
  });

  it("closes on cancel click", () => {
    render(<ParentsAdminClient />);
    fireEvent.click(screen.getByTestId("delete-p1"));
    fireEvent.click(screen.getByTestId("cancel-delete"));
    expect(screen.queryByTestId("confirm-dialog")).toBeNull();
  });
});

describe("ParentsAdminClient — data display", () => {
  it("renders parent name from mock data", () => {
    render(<ParentsAdminClient />);
    expect(screen.getByText("Carol Parent")).toBeInTheDocument();
  });
});
