import { describe, it, expect, vi, beforeEach } from "vitest";
import axios from "axios";
import { useAuthStore } from "@/store/auth.store";
import type { AuthUser } from "@/types/auth";

// ── mock authService (intercepted by dynamic import inside the store) ─────────
const mockGetSession = vi.fn();
const mockRefresh = vi.fn();
const mockLogout = vi.fn();

vi.mock("@/services/auth.service", () => ({
  authService: {
    getSession: mockGetSession,
    refresh: mockRefresh,
    logout: mockLogout
  }
}));

// ── helpers ───────────────────────────────────────────────────────────────────
const makeUser = (overrides: Partial<AuthUser> = {}): AuthUser => ({
  id: "u1",
  email: "admin@school.com",
  fullName: "Admin User",
  role: "Admin",
  ...overrides
});

function make401Error() {
  const err = new axios.AxiosError("Unauthorized");
  err.response = { status: 401, data: {}, headers: {}, config: {} as never, statusText: "Unauthorized" };
  return err;
}

function makeNetworkError() {
  return new Error("Network error");
}

// Reset store to known initial state before every test
beforeEach(() => {
  vi.clearAllMocks();
  useAuthStore.setState({
    user: null,
    isAuthenticated: false,
    hasInitialized: false
  });
});

// ── initializeSession ─────────────────────────────────────────────────────────
describe("useAuthStore — initializeSession", () => {
  it("sets user, isAuthenticated, and hasInitialized when a user is passed", () => {
    const user = makeUser();
    useAuthStore.getState().initializeSession(user);
    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.isAuthenticated).toBe(true);
    expect(state.hasInitialized).toBe(true);
  });

  it("sets isAuthenticated=false and hasInitialized when null is passed", () => {
    useAuthStore.getState().initializeSession(null);
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.hasInitialized).toBe(true);
  });
});

// ── hydrateSession ────────────────────────────────────────────────────────────
describe("useAuthStore — hydrateSession", () => {
  it("sets user when getSession succeeds", async () => {
    const user = makeUser();
    mockGetSession.mockResolvedValueOnce(user);

    const result = await useAuthStore.getState().hydrateSession();

    expect(result).toEqual(user);
    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.isAuthenticated).toBe(true);
    expect(state.hasInitialized).toBe(true);
  });

  it("falls back to refresh when getSession returns 401 and refresh succeeds", async () => {
    const user = makeUser();
    mockGetSession.mockRejectedValueOnce(make401Error());
    mockRefresh.mockResolvedValueOnce(user);

    const result = await useAuthStore.getState().hydrateSession();

    expect(result).toEqual(user);
    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.isAuthenticated).toBe(true);
    expect(state.hasInitialized).toBe(true);
  });

  it("clears session and returns null when getSession returns 401 and refresh also fails", async () => {
    mockGetSession.mockRejectedValueOnce(make401Error());
    mockRefresh.mockRejectedValueOnce(new Error("Refresh token expired"));

    const result = await useAuthStore.getState().hydrateSession();

    expect(result).toBeNull();
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.hasInitialized).toBe(true);
  });

  it("clears state and rethrows on non-401 errors from getSession", async () => {
    const networkError = makeNetworkError();
    mockGetSession.mockRejectedValueOnce(networkError);

    await expect(useAuthStore.getState().hydrateSession()).rejects.toThrow("Network error");

    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.hasInitialized).toBe(true);
  });

  it("does not call refresh on non-401 axios errors", async () => {
    const err = new axios.AxiosError("Server Error");
    err.response = { status: 500, data: {}, headers: {}, config: {} as never, statusText: "Internal Server Error" };
    mockGetSession.mockRejectedValueOnce(err);

    await expect(useAuthStore.getState().hydrateSession()).rejects.toThrow();
    expect(mockRefresh).not.toHaveBeenCalled();
  });
});

// ── refreshSession ────────────────────────────────────────────────────────────
describe("useAuthStore — refreshSession", () => {
  it("updates user state when refresh succeeds", async () => {
    const user = makeUser();
    mockRefresh.mockResolvedValueOnce(user);

    await useAuthStore.getState().refreshSession();

    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.isAuthenticated).toBe(true);
    expect(state.hasInitialized).toBe(true);
  });

  it("propagates error when refresh fails", async () => {
    mockRefresh.mockRejectedValueOnce(new Error("Invalid token"));

    await expect(useAuthStore.getState().refreshSession()).rejects.toThrow("Invalid token");
  });
});

// ── clearSession ──────────────────────────────────────────────────────────────
describe("useAuthStore — clearSession", () => {
  it("clears user and marks as not authenticated", () => {
    useAuthStore.setState({ user: makeUser(), isAuthenticated: true, hasInitialized: true });
    useAuthStore.getState().clearSession();

    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.hasInitialized).toBe(true);
  });
});

// ── logout ────────────────────────────────────────────────────────────────────
describe("useAuthStore — logout", () => {
  it("calls authService.logout and clears session on success", async () => {
    useAuthStore.setState({ user: makeUser(), isAuthenticated: true, hasInitialized: true });
    mockLogout.mockResolvedValueOnce(undefined);

    await useAuthStore.getState().logout();

    expect(mockLogout).toHaveBeenCalledOnce();
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });

  it("still clears session even when authService.logout throws (finally block)", async () => {
    useAuthStore.setState({ user: makeUser(), isAuthenticated: true, hasInitialized: true });
    mockLogout.mockRejectedValueOnce(new Error("Logout API down"));

    // try/finally without catch: error propagates but finally block still runs
    await expect(useAuthStore.getState().logout()).rejects.toThrow("Logout API down");

    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });
});
