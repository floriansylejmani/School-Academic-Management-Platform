import { describe, it, expect, vi, beforeEach } from "vitest";
import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { NotificationsAdminClient } from "@/features/notifications/components/notifications-admin-client";

// ── shared mutation handle ────────────────────────────────────────────────────
const mockMutateAsync = vi.fn();
let sendIsPending = false;

vi.mock("@/features/notifications/hooks/use-notifications", () => ({
  useSendNotification: () => ({
    mutateAsync: mockMutateAsync,
    isPending: sendIsPending
  })
}));

// ── user-list query mocks ─────────────────────────────────────────────────────
let teachersState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      { userId: "tu1", fullName: "Alice Teacher", email: "alice@school.com" }
    ]
  }
};

let studentsState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      { userId: "su1", fullName: "Bob Student", email: "bob@school.com" }
    ]
  }
};

let parentsState = {
  isLoading: false,
  isError: false,
  data: {
    items: [
      { userId: "pu1", fullName: "Carol Parent", email: "carol@school.com" }
    ]
  }
};

vi.mock("@/features/teachers/hooks/use-teachers", () => ({
  useTeachers: () => teachersState
}));
vi.mock("@/features/students/hooks/use-students", () => ({
  useStudents: () => studentsState
}));
vi.mock("@/features/parents/hooks/use-parents", () => ({
  useParents: () => parentsState
}));

// ── minimal UI stubs ──────────────────────────────────────────────────────────
vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ title }: { title: string }) => (
    <h1 data-testid="page-title">{title}</h1>
  )
}));

vi.mock("@/components/ui/card", () => ({
  Card: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="card">{children}</div>
  )
}));

vi.mock("@/components/ui/button", () => ({
  Button: ({
    children,
    onClick,
    disabled,
    type,
    variant
  }: {
    children: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
    type?: "button" | "submit" | "reset";
    variant?: string;
  }) => (
    <button
      type={type ?? "button"}
      onClick={onClick}
      disabled={disabled}
      data-variant={variant}
    >
      {children}
    </button>
  )
}));

vi.mock("@/components/ui/form-field", () => ({
  FormField: ({
    label,
    error,
    children
  }: {
    label: string;
    error?: string;
    children: React.ReactNode;
  }) => (
    <div>
      <label>{label}</label>
      {children}
      {error && <span data-testid="field-error">{error}</span>}
    </div>
  )
}));

vi.mock("@/components/ui/input", () => ({
  Input: function MockInput(props: React.InputHTMLAttributes<HTMLInputElement>) {
    return <input data-testid="title-input" {...props} />;
  }
}));

vi.mock("@/components/ui/select", () => ({
  Select: function MockSelect({
    children,
    placeholder,
    ...rest
  }: React.SelectHTMLAttributes<HTMLSelectElement> & { placeholder?: string }) {
    return (
      <select data-testid="recipient-select" {...rest}>
        {placeholder ? <option value="">{placeholder}</option> : null}
        {children}
      </select>
    );
  }
}));

vi.mock("lucide-react", () => ({
  Send: () => <span data-testid="icon-send" />
}));

const MESSAGE_PLACEHOLDER = /Full notification text/i;

// ── Tests ─────────────────────────────────────────────────────────────────────
describe("NotificationsAdminClient — initial state", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sendIsPending = false;
    teachersState = { isLoading: false, isError: false, data: { items: [{ userId: "tu1", fullName: "Alice Teacher", email: "alice@school.com" }] } };
    studentsState = { isLoading: false, isError: false, data: { items: [{ userId: "su1", fullName: "Bob Student", email: "bob@school.com" }] } };
    parentsState = { isLoading: false, isError: false, data: { items: [{ userId: "pu1", fullName: "Carol Parent", email: "carol@school.com" }] } };
  });

  it("renders the page title", () => {
    render(<NotificationsAdminClient />);
    expect(screen.getByTestId("page-title")).toHaveTextContent("Send Notifications");
  });

  it("shows Broadcast to role and Single user mode buttons", () => {
    render(<NotificationsAdminClient />);
    expect(screen.getByText("Broadcast to role")).toBeInTheDocument();
    expect(screen.getByText("Single user")).toBeInTheDocument();
  });

  it("defaults to role broadcast mode (role radio buttons visible)", () => {
    render(<NotificationsAdminClient />);
    expect(screen.getByText("Admin")).toBeInTheDocument();
    expect(screen.getByText("Teacher")).toBeInTheDocument();
    expect(screen.getByText("Student")).toBeInTheDocument();
    expect(screen.getByText("Parent")).toBeInTheDocument();
  });

  it("does not show recipient select in role mode", () => {
    render(<NotificationsAdminClient />);
    expect(screen.queryByTestId("recipient-select")).toBeNull();
  });
});

describe("NotificationsAdminClient — mode switching", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sendIsPending = false;
    teachersState = { isLoading: false, isError: false, data: { items: [{ userId: "tu1", fullName: "Alice Teacher", email: "alice@school.com" }] } };
    studentsState = { isLoading: false, isError: false, data: { items: [{ userId: "su1", fullName: "Bob Student", email: "bob@school.com" }] } };
    parentsState = { isLoading: false, isError: false, data: { items: [{ userId: "pu1", fullName: "Carol Parent", email: "carol@school.com" }] } };
  });

  it("switches to single-user mode and shows the recipient select", () => {
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.getByTestId("recipient-select")).toBeInTheDocument();
  });

  it("hides role radio buttons after switching to single-user mode", () => {
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.queryByText("Admin")).toBeNull();
  });

  it("restores role radio buttons when switching back to role mode", () => {
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    fireEvent.click(screen.getByText("Broadcast to role"));
    expect(screen.getByText("Admin")).toBeInTheDocument();
    expect(screen.queryByTestId("recipient-select")).toBeNull();
  });

  it("lists teachers, students and parents as recipient options", () => {
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    // All 3 user types appear in the select
    expect(screen.getByText(/Alice Teacher/)).toBeInTheDocument();
    expect(screen.getByText(/Bob Student/)).toBeInTheDocument();
    expect(screen.getByText(/Carol Parent/)).toBeInTheDocument();
  });
});

describe("NotificationsAdminClient — recipient loading / error states", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows loading message when teacher query is loading", () => {
    teachersState = { isLoading: true, isError: false, data: { items: [] } };
    studentsState = { isLoading: false, isError: false, data: { items: [] } };
    parentsState = { isLoading: false, isError: false, data: { items: [] } };
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.getByText(/Loading available recipients/i)).toBeInTheDocument();
  });

  it("shows error message when student query fails", () => {
    teachersState = { isLoading: false, isError: false, data: { items: [] } };
    studentsState = { isLoading: false, isError: true, data: { items: [] } };
    parentsState = { isLoading: false, isError: false, data: { items: [] } };
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.getByText(/user list could not be loaded/i)).toBeInTheDocument();
  });

  it("shows empty message when no users available", () => {
    teachersState = { isLoading: false, isError: false, data: { items: [] } };
    studentsState = { isLoading: false, isError: false, data: { items: [] } };
    parentsState = { isLoading: false, isError: false, data: { items: [] } };
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.getByText(/no teacher, student, or parent accounts/i)).toBeInTheDocument();
  });

  it("disables submit button when in user mode and recipients are loading", () => {
    teachersState = { isLoading: true, isError: false, data: { items: [] } };
    studentsState = { isLoading: false, isError: false, data: { items: [] } };
    parentsState = { isLoading: false, isError: false, data: { items: [] } };
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    const submitBtn = screen.getByText("Send notification");
    expect(submitBtn).toBeDisabled();
  });

  it("disables submit button when in user mode and recipient list errored", () => {
    teachersState = { isLoading: false, isError: true, data: { items: [] } };
    studentsState = { isLoading: false, isError: false, data: { items: [] } };
    parentsState = { isLoading: false, isError: false, data: { items: [] } };
    render(<NotificationsAdminClient />);
    fireEvent.click(screen.getByText("Single user"));
    expect(screen.getByText("Send notification")).toBeDisabled();
  });
});

describe("NotificationsAdminClient — form submission", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sendIsPending = false;
    teachersState = { isLoading: false, isError: false, data: { items: [{ userId: "tu1", fullName: "Alice Teacher", email: "alice@school.com" }] } };
    studentsState = { isLoading: false, isError: false, data: { items: [{ userId: "su1", fullName: "Bob Student", email: "bob@school.com" }] } };
    parentsState = { isLoading: false, isError: false, data: { items: [{ userId: "pu1", fullName: "Carol Parent", email: "carol@school.com" }] } };
  });

  it("calls mutateAsync with roleName when broadcasting to a role", async () => {
    mockMutateAsync.mockResolvedValueOnce({});
    render(<NotificationsAdminClient />);

    // Fill title + message
    fireEvent.change(screen.getByTestId("title-input"), { target: { value: "School closure" } });
    fireEvent.change(screen.getByPlaceholderText(MESSAGE_PLACEHOLDER), {
      target: { value: "Tomorrow is closed." }
    });

    // Submit
    fireEvent.click(screen.getByText("Send notification"));

    await waitFor(() =>
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "School closure",
          message: "Tomorrow is closed.",
          roleName: expect.any(String),
          userId: undefined
        })
      )
    );
  });

  it("calls mutateAsync with userId when targeting a single user", async () => {
    mockMutateAsync.mockResolvedValueOnce({});
    render(<NotificationsAdminClient />);

    fireEvent.click(screen.getByText("Single user"));
    fireEvent.change(screen.getByTestId("recipient-select"), { target: { value: "tu1" } });
    fireEvent.change(screen.getByTestId("title-input"), { target: { value: "Direct note" } });
    fireEvent.change(screen.getByPlaceholderText(MESSAGE_PLACEHOLDER), {
      target: { value: "Just for you." }
    });

    fireEvent.click(screen.getByText("Send notification"));

    await waitFor(() =>
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "Direct note",
          message: "Just for you.",
          userId: "tu1",
          roleName: undefined
        })
      )
    );
  });

  it("component remains mounted after a successful submission", async () => {
    mockMutateAsync.mockResolvedValueOnce({});
    render(<NotificationsAdminClient />);

    fireEvent.change(screen.getByTestId("title-input"), { target: { value: "Title" } });
    fireEvent.change(screen.getByPlaceholderText(MESSAGE_PLACEHOLDER), {
      target: { value: "Message." }
    });
    fireEvent.click(screen.getByText("Send notification"));

    await waitFor(() => expect(mockMutateAsync).toHaveBeenCalled());
    // Form stays rendered (no navigation away)
    expect(screen.getByTestId("page-title")).toBeInTheDocument();
  });

  it("does not reset the form after a failed submission", async () => {
    mockMutateAsync.mockRejectedValueOnce(new Error("Server error"));
    render(<NotificationsAdminClient />);

    fireEvent.change(screen.getByTestId("title-input"), { target: { value: "Keep this" } });
    fireEvent.change(screen.getByPlaceholderText(MESSAGE_PLACEHOLDER), {
      target: { value: "Also keep." }
    });
    fireEvent.click(screen.getByText("Send notification"));

    await waitFor(() => expect(mockMutateAsync).toHaveBeenCalled());
    expect(screen.getByTestId("title-input")).toHaveValue("Keep this");
  });
});
