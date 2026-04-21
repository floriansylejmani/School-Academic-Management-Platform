import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createElement } from "react";
import { useCreateTeacher, useUpdateTeacher, useDeleteTeacher } from "@/features/teachers/hooks/use-teachers";

// ── service mock ───────────────────────────────────────────────────────────────
vi.mock("@/services/teachers.service", () => ({
  teachersService: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn()
  }
}));

// ── toast mock ─────────────────────────────────────────────────────────────────
const mockSuccess = vi.fn();
const mockError = vi.fn();

vi.mock("@/hooks/use-toast", () => ({
  useToast: () => ({ success: mockSuccess, error: mockError })
}));

// ── zustand store mock (required by use-toast indirectly) ──────────────────────
vi.mock("@/store/toast.store", () => ({
  useToastStore: () => vi.fn()
}));

// ── imports after mocks ────────────────────────────────────────────────────────
import { teachersService } from "@/services/teachers.service";

const mockService = teachersService as {
  create: ReturnType<typeof vi.fn>;
  update: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
};

// ── helpers ────────────────────────────────────────────────────────────────────
function makeWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  });

  function Wrapper({ children }: { children: React.ReactNode }) {
    return createElement(QueryClientProvider, { client }, children);
  }

  Wrapper.displayName = "QueryClientTestWrapper";
  return Wrapper;
}

function makeAxiosError(message: string, status = 400) {
  const err = Object.assign(new Error(message), {
    isAxiosError: true,
    response: {
      status,
      data: { message, errors: null }
    }
  });
  return err;
}

// ── tests ──────────────────────────────────────────────────────────────────────
describe("useCreateTeacher", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls toast.success and invalidates query on successful create", async () => {
    const fakeTeacher = { id: "t1", fullName: "Jane Smith" };
    mockService.create.mockResolvedValueOnce(fakeTeacher);

    const { result } = renderHook(() => useCreateTeacher(), {
      wrapper: makeWrapper()
    });

    await result.current.mutateAsync({ fullName: "Jane Smith" } as never);

    await waitFor(() => expect(mockSuccess).toHaveBeenCalledTimes(1));
    expect(mockSuccess).toHaveBeenCalledWith(
      "Teacher created",
      "The teacher record was saved successfully."
    );
    expect(mockError).not.toHaveBeenCalled();
  });

  it("calls toast.error with API message on failed create", async () => {
    mockService.create.mockRejectedValueOnce(
      makeAxiosError("A user with this email already exists.")
    );

    const { result } = renderHook(() => useCreateTeacher(), {
      wrapper: makeWrapper()
    });

    await expect(result.current.mutateAsync({ fullName: "Jane Smith" } as never)).rejects.toThrow();

    await waitFor(() => expect(mockError).toHaveBeenCalledTimes(1));
    expect(mockError).toHaveBeenCalledWith(
      "Unable to create teacher",
      "A user with this email already exists."
    );
    expect(mockSuccess).not.toHaveBeenCalled();
  });

  it("re-throws on error so the caller try-catch receives it", async () => {
    mockService.create.mockRejectedValueOnce(
      makeAxiosError("Something went wrong.")
    );

    const { result } = renderHook(() => useCreateTeacher(), {
      wrapper: makeWrapper()
    });

    await expect(
      result.current.mutateAsync({ fullName: "Test" } as never)
    ).rejects.toBeDefined();
  });
});

describe("useUpdateTeacher", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls toast.success on successful update", async () => {
    const fakeTeacher = { id: "t1", fullName: "Jane Updated" };
    mockService.update.mockResolvedValueOnce(fakeTeacher);

    const { result } = renderHook(() => useUpdateTeacher(), {
      wrapper: makeWrapper()
    });

    await result.current.mutateAsync({ id: "t1", payload: { fullName: "Jane Updated" } as never });

    await waitFor(() => expect(mockSuccess).toHaveBeenCalledTimes(1));
    expect(mockSuccess).toHaveBeenCalledWith(
      "Teacher updated",
      "The teacher record was updated successfully."
    );
  });

  it("calls toast.error on failed update", async () => {
    mockService.update.mockRejectedValueOnce(makeAxiosError("Teacher not found.", 404));

    const { result } = renderHook(() => useUpdateTeacher(), {
      wrapper: makeWrapper()
    });

    await expect(
      result.current.mutateAsync({ id: "t1", payload: {} as never })
    ).rejects.toBeDefined();

    await waitFor(() => expect(mockError).toHaveBeenCalledTimes(1));
    expect(mockError).toHaveBeenCalledWith("Unable to update teacher", "Teacher not found.");
  });
});

describe("useDeleteTeacher", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls toast.success on successful delete", async () => {
    mockService.remove.mockResolvedValueOnce(undefined);

    const { result } = renderHook(() => useDeleteTeacher(), {
      wrapper: makeWrapper()
    });

    await result.current.mutateAsync("t1");

    await waitFor(() => expect(mockSuccess).toHaveBeenCalledTimes(1));
    expect(mockSuccess).toHaveBeenCalledWith("Teacher deleted", "The teacher record was removed.");
  });

  it("calls toast.error on failed delete", async () => {
    mockService.remove.mockRejectedValueOnce(
      makeAxiosError("You do not have permission to perform this action.", 403)
    );

    const { result } = renderHook(() => useDeleteTeacher(), {
      wrapper: makeWrapper()
    });

    await expect(result.current.mutateAsync("t1")).rejects.toBeDefined();

    await waitFor(() => expect(mockError).toHaveBeenCalledTimes(1));
    expect(mockError).toHaveBeenCalledWith(
      "Unable to delete teacher",
      "You do not have permission to perform this action."
    );
  });
});
