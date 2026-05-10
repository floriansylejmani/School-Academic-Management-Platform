export const DEFAULT_PAGE_NUMBER = 1;
export const DEFAULT_PAGE_SIZE = 10;
export const MAX_PAGE_SIZE = 100;

function normalizeInteger(value: unknown, fallback: number, min: number, max?: number) {
  const parsed = typeof value === "number" ? value : Number(value);
  const normalized = Number.isFinite(parsed) ? Math.floor(parsed) : fallback;
  const lowerBounded = Math.max(min, normalized);

  return max === undefined ? lowerBounded : Math.min(max, lowerBounded);
}

export function normalizePaginationParams<T>(params: T): T {
  if (!params) {
    return params;
  }

  if (params instanceof URLSearchParams) {
    const normalized = new URLSearchParams(params);

    if (normalized.has("pageNumber")) {
      normalized.set(
        "pageNumber",
        String(normalizeInteger(normalized.get("pageNumber"), DEFAULT_PAGE_NUMBER, 1))
      );
    }

    if (normalized.has("pageSize")) {
      normalized.set(
        "pageSize",
        String(normalizeInteger(normalized.get("pageSize"), DEFAULT_PAGE_SIZE, 1, MAX_PAGE_SIZE))
      );
    }

    return normalized as T;
  }

  if (typeof params !== "object") {
    return params;
  }

  const paginationParams = params as Record<string, unknown>;
  const normalizedParams = { ...paginationParams };

  if ("pageNumber" in normalizedParams) {
    normalizedParams.pageNumber = normalizeInteger(normalizedParams.pageNumber, DEFAULT_PAGE_NUMBER, 1);
  }

  if ("pageSize" in normalizedParams) {
    normalizedParams.pageSize = normalizeInteger(normalizedParams.pageSize, DEFAULT_PAGE_SIZE, 1, MAX_PAGE_SIZE);
  }

  return normalizedParams as T;
}
