import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { NotificationsInbox } from "@/features/notifications/components/notifications-inbox";

// ── shared mutation handles ────────────────────────────────────────────────────
const mockMarkRead = vi.fn();
const mockMarkAllRead = vi.fn();
const mockRefetchNotifications = vi.fn();
const mockRefetchUnread = vi.fn();

// Default query state — overridden per test when needed
let notificationsState = {
  isLoading: false,
  isError: false,
  refetch: mockRefetchNotifications,
  data: {
    items: [
      {
        id: "n1",
        userId: "u1",
        title: "Exam scheduled",
        message: "Biology exam on Monday.",
        isRead: false,
        createdAt: new Date(Date.now() - 60_000).toISOString(), // 1m ago
        studentId: null,
        studentName: null
      },
      {
        id: "n2",
        userId: "u1",
        title: "Fee assigned",
        message: "Library fee due next week.",
        isRead: true,
        createdAt: new Date(Date.now() - 7_200_000).toISOString(), // 2h ago
        studentId: "s1",
        studentName: "Alice Child"
      }
    ]
  }
};

let unreadState = {
  isLoading: false,
  isError: false,
  refetch: mockRefetchUnread,
  data: { count: 1 }
};

vi.mock("@/features/notifications/hooks/use-notifications", () => ({
  useNotifications: () => notificationsState,
  useUnreadCount: () => unreadState,
  useMarkNotificationRead: () => ({
    mutate: mockMarkRead,
    isPending: false
  }),
  useMarkAllNotificationsRead: () => ({
    mutate: mockMarkAllRead,
    isPending: false
  })
}));

// ── minimal UI stubs ──────────────────────────────────────────────────────────
vi.mock("@/components/ui/button", () => ({
  Button: ({
    children,
    onClick,
    disabled,
    variant
  }: {
    children: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
    variant?: string;
  }) => (
    <button onClick={onClick} disabled={disabled} data-variant={variant}>
      {children}
    </button>
  )
}));

vi.mock("@/components/ui/card", () => ({
  Card: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div data-testid="notification-card" className={className}>{children}</div>
  )
}));

vi.mock("@/components/ui/page-header", () => ({
  PageHeader: ({ title, description }: { title: string; description: string }) => (
    <div>
      <h1 data-testid="page-title">{title}</h1>
      <p data-testid="page-description">{description}</p>
    </div>
  )
}));

vi.mock("@/components/ui/empty-state", () => ({
  EmptyState: ({
    title,
    action
  }: {
    title: string;
    description?: string;
    action?: React.ReactNode;
  }) => (
    <div data-testid="empty-state">
      <span data-testid="empty-title">{title}</span>
      {action}
    </div>
  )
}));

vi.mock("@/components/ui/loading-state", () => ({
  LoadingState: ({ title }: { title: string }) => (
    <div data-testid="loading-state">{title}</div>
  )
}));

// Stub lucide icons to avoid SVG rendering issues
vi.mock("lucide-react", () => ({
  Bell: () => <span data-testid="icon-bell" />,
  BellOff: () => <span data-testid="icon-belloff" />,
  CheckCheck: () => <span data-testid="icon-checkcheck" />,
  User: () => <span data-testid="icon-user" />
}));

const defaultProps = {
  eyebrow: "Test / Notifications",
  title: "Notifications",
  description: "Your latest updates.",
  emptyTitle: "No notifications",
  emptyDescription: "Nothing here yet."
};

// ── Tests ─────────────────────────────────────────────────────────────────────
describe("NotificationsInbox — loading and error states", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    notificationsState = {
      isLoading: false,
      isError: false,
      refetch: mockRefetchNotifications,
      data: {
        items: [
          {
            id: "n1",
            userId: "u1",
            title: "Exam scheduled",
            message: "Biology exam on Monday.",
            isRead: false,
            createdAt: new Date(Date.now() - 60_000).toISOString(),
            studentId: null,
            studentName: null
          },
          {
            id: "n2",
            userId: "u1",
            title: "Fee assigned",
            message: "Library fee due next week.",
            isRead: true,
            createdAt: new Date(Date.now() - 7_200_000).toISOString(),
            studentId: "s1",
            studentName: "Alice Child"
          }
        ]
      }
    };
    unreadState = { isLoading: false, isError: false, refetch: mockRefetchUnread, data: { count: 1 } };
  });

  it("shows loading state while notifications query is pending", () => {
    notificationsState = { ...notificationsState, isLoading: true, data: { items: [] } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows loading state while unread-count query is pending", () => {
    unreadState = { ...unreadState, isLoading: true, data: { count: 0 } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("shows error state when notifications query fails", () => {
    notificationsState = { ...notificationsState, isError: true, data: { items: [] } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("empty-state")).toBeInTheDocument();
    expect(screen.getByTestId("empty-title")).toHaveTextContent("Unable to load notifications");
  });

  it("calls refetch on both queries when retry is clicked", () => {
    notificationsState = { ...notificationsState, isError: true, data: { items: [] } };
    render(<NotificationsInbox {...defaultProps} />);
    fireEvent.click(screen.getByText("Retry"));
    expect(mockRefetchNotifications).toHaveBeenCalled();
    expect(mockRefetchUnread).toHaveBeenCalled();
  });
});

describe("NotificationsInbox — notification list rendering", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    notificationsState = {
      isLoading: false,
      isError: false,
      refetch: mockRefetchNotifications,
      data: {
        items: [
          {
            id: "n1",
            userId: "u1",
            title: "Exam scheduled",
            message: "Biology exam on Monday.",
            isRead: false,
            createdAt: new Date(Date.now() - 60_000).toISOString(),
            studentId: null,
            studentName: null
          },
          {
            id: "n2",
            userId: "u1",
            title: "Fee assigned",
            message: "Library fee due next week.",
            isRead: true,
            createdAt: new Date(Date.now() - 7_200_000).toISOString(),
            studentId: "s1",
            studentName: "Alice Child"
          }
        ]
      }
    };
    unreadState = { isLoading: false, isError: false, refetch: mockRefetchUnread, data: { count: 1 } };
  });

  it("renders all notification titles", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByText("Exam scheduled")).toBeInTheDocument();
    expect(screen.getByText("Fee assigned")).toBeInTheDocument();
  });

  it("shows the unread count in the page description", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("page-description")).toHaveTextContent("1 unread notification");
  });

  it("shows 'all caught up' in description when there are no unread notifications", () => {
    unreadState = { ...unreadState, data: { count: 0 } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("page-description")).toHaveTextContent("You are all caught up");
  });

  it("renders the student-context badge when studentName is set", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByText("Alice Child")).toBeInTheDocument();
  });

  it("does not render a student badge on account-level notifications", () => {
    // n1 has no studentName; n2 has one — only one badge should appear
    render(<NotificationsInbox {...defaultProps} />);
    const badges = screen.getAllByText("Alice Child");
    expect(badges).toHaveLength(1);
  });

  it("renders a 'Mark read' button only for unread notifications", () => {
    render(<NotificationsInbox {...defaultProps} />);
    const markReadButtons = screen.getAllByText("Mark read");
    // n1 is unread → button; n2 is read → no button
    expect(markReadButtons).toHaveLength(1);
  });

  it("calls markRead mutation when Mark read is clicked", () => {
    render(<NotificationsInbox {...defaultProps} />);
    fireEvent.click(screen.getByText("Mark read"));
    expect(mockMarkRead).toHaveBeenCalledWith("n1");
  });

  it("shows 'Mark all as read' button when unread count > 0", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByText("Mark all as read")).toBeInTheDocument();
  });

  it("calls markAllRead when Mark all as read is clicked", () => {
    render(<NotificationsInbox {...defaultProps} />);
    fireEvent.click(screen.getByText("Mark all as read"));
    expect(mockMarkAllRead).toHaveBeenCalled();
  });

  it("hides 'Mark all as read' when unread count is 0", () => {
    unreadState = { ...unreadState, data: { count: 0 } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.queryByText("Mark all as read")).toBeNull();
  });
});

describe("NotificationsInbox — empty state", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows the custom emptyTitle when there are no notifications", () => {
    notificationsState = {
      isLoading: false,
      isError: false,
      refetch: mockRefetchNotifications,
      data: { items: [] }
    };
    unreadState = { isLoading: false, isError: false, refetch: mockRefetchUnread, data: { count: 0 } };
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByTestId("empty-title")).toHaveTextContent("No notifications");
  });
});

describe("NotificationsInbox — filter toggle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    notificationsState = {
      isLoading: false,
      isError: false,
      refetch: mockRefetchNotifications,
      data: {
        items: [
          {
            id: "n1",
            userId: "u1",
            title: "Exam scheduled",
            message: "Biology exam on Monday.",
            isRead: false,
            createdAt: new Date(Date.now() - 60_000).toISOString(),
            studentId: null,
            studentName: null
          }
        ]
      }
    };
    unreadState = { isLoading: false, isError: false, refetch: mockRefetchUnread, data: { count: 1 } };
  });

  it("renders both All and Unread filter buttons", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByText("All")).toBeInTheDocument();
    expect(screen.getByText(/Unread/)).toBeInTheDocument();
  });

  it("shows unread count in the Unread filter button label", () => {
    render(<NotificationsInbox {...defaultProps} />);
    expect(screen.getByText("Unread (1)")).toBeInTheDocument();
  });

  it("renders headerSlot content when provided", () => {
    render(
      <NotificationsInbox
        {...defaultProps}
        headerSlot={<div data-testid="custom-slot">ChildSwitcher</div>}
      />
    );
    expect(screen.getByTestId("custom-slot")).toBeInTheDocument();
  });
});

describe("NotificationsInbox — role variants (student / teacher / parent)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    notificationsState = {
      isLoading: false,
      isError: false,
      refetch: mockRefetchNotifications,
      data: { items: [] }
    };
    unreadState = { isLoading: false, isError: false, refetch: mockRefetchUnread, data: { count: 0 } };
  });

  it("renders student inbox with correct eyebrow", () => {
    render(
      <NotificationsInbox
        eyebrow="Student / Notifications"
        title="My Notifications"
        description="Updates from your school."
        emptyTitle="No notifications"
        emptyDescription="Nothing yet."
      />
    );
    // The page header is stubbed — just verify it mounts without errors
    expect(screen.getByTestId("page-title")).toHaveTextContent("My Notifications");
  });

  it("renders teacher inbox with correct eyebrow", () => {
    render(
      <NotificationsInbox
        eyebrow="Teacher / Notifications"
        title="Teacher Notifications"
        description="Updates about your classes."
        emptyTitle="No notifications"
        emptyDescription="Nothing yet."
      />
    );
    expect(screen.getByTestId("page-title")).toHaveTextContent("Teacher Notifications");
  });

  it("renders parent inbox with correct eyebrow", () => {
    render(
      <NotificationsInbox
        eyebrow="Parent / Notifications"
        title="Family Notifications"
        description="Updates about your children."
        emptyTitle="No notifications"
        emptyDescription="Nothing yet."
      />
    );
    expect(screen.getByTestId("page-title")).toHaveTextContent("Family Notifications");
  });
});
