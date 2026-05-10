import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginForm } from "@/features/auth/login-form";
import { authService } from "@/services/auth.service";
import { useAuthStore } from "@/store/auth.store";
import type { AuthUser } from "@/types/auth";

const mockReplace = vi.fn();
const mockRefresh = vi.fn();
const mockInitializeSession = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    replace: mockReplace,
    refresh: mockRefresh
  })
}));

vi.mock("@/services/auth.service", () => ({
  authService: {
    login: vi.fn()
  }
}));

vi.mock("@/store/auth.store", () => ({
  useAuthStore: vi.fn()
}));

const mockedLogin = vi.mocked(authService.login);
const mockedUseAuthStore = vi.mocked(useAuthStore);

function renderWithQueryClient(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

function mockAuthStore() {
  mockedUseAuthStore.mockImplementation((selector) =>
    selector({
      user: null,
      isAuthenticated: false,
      hasInitialized: false,
      initializeSession: mockInitializeSession,
      hydrateSession: vi.fn(),
      refreshSession: vi.fn(),
      clearSession: vi.fn(),
      logout: vi.fn()
    })
  );
}

describe("LoginForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAuthStore();
  });

  it("renders email and password fields with the sign-in button", () => {
    renderWithQueryClient(<LoginForm />);

    expect(screen.getByRole("heading", { name: /sign in to your workspace/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("shows validation errors when email and password are empty", async () => {
    renderWithQueryClient(<LoginForm />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "" } });

    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText("Email is required")).toBeInTheDocument();
    expect(await screen.findByText("Password is required")).toBeInTheDocument();
    expect(mockedLogin).not.toHaveBeenCalled();
  });

  it("logs in successfully and redirects admin to admin dashboard", async () => {
    const adminUser: AuthUser = {
      id: "admin-user-id",
      fullName: "Admin User",
      email: "admin@school.com",
      role: "Admin"
    };

    mockedLogin.mockResolvedValueOnce(adminUser);

    renderWithQueryClient(<LoginForm />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "admin@school.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "Admin@12345" } });

    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(mockedLogin).toHaveBeenCalled();
    });

    expect(mockedLogin.mock.calls[0][0]).toEqual({
      email: "admin@school.com",
      password: "Admin@12345"
    });

    expect(mockInitializeSession).toHaveBeenCalledWith(adminUser);
    expect(mockReplace).toHaveBeenCalledWith("/admin/dashboard");
    expect(mockRefresh).toHaveBeenCalled();
  });

  it("redirects teacher, student, and parent users to their role dashboards", async () => {
    const cases: Array<{ user: AuthUser; path: string }> = [
      {
        user: {
          id: "teacher-user-id",
          fullName: "Teacher User",
          email: "teacher@school.com",
          role: "Teacher"
        },
        path: "/teacher/dashboard"
      },
      {
        user: {
          id: "student-user-id",
          fullName: "Student User",
          email: "student@school.com",
          role: "Student"
        },
        path: "/student/dashboard"
      },
      {
        user: {
          id: "parent-user-id",
          fullName: "Parent User",
          email: "parent@school.com",
          role: "Parent"
        },
        path: "/parent/dashboard"
      }
    ];

    for (const item of cases) {
      vi.clearAllMocks();
      mockAuthStore();

      mockedLogin.mockResolvedValueOnce(item.user);

      const { unmount } = renderWithQueryClient(<LoginForm />);

      fireEvent.change(screen.getByLabelText(/email/i), {
        target: { value: item.user.email }
      });
      fireEvent.change(screen.getByLabelText(/password/i), {
        target: { value: "Password@123" }
      });

      fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

      await waitFor(() => {
        expect(mockReplace).toHaveBeenCalledWith(item.path);
      });

      expect(mockInitializeSession).toHaveBeenCalledWith(item.user);

      unmount();
    }
  });

  it("shows an error message when login fails", async () => {
    mockedLogin.mockRejectedValueOnce(new Error("Invalid credentials"));

    renderWithQueryClient(<LoginForm />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "wrong@school.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "WrongPass123" } });

    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText(/invalid credentials/i)).toBeInTheDocument();
    expect(mockReplace).not.toHaveBeenCalled();
    expect(mockInitializeSession).not.toHaveBeenCalled();
  });
});
