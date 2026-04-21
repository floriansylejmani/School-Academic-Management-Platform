const normalizedApiBaseUrl = (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api").replace(/\/+$/, "");

// Browser (CSR) URL — always the public, host-accessible address.
export const API_BASE_URL = normalizedApiBaseUrl;

// Server-side (SSR / Next.js process) URL.
// In Docker the Next.js container reaches the backend via the internal network
// name "api", so API_URL should be set to http://api:8080/api there.
// Locally it falls back to the same value as API_BASE_URL.
export const SERVER_API_BASE_URL = (
  process.env.API_URL ?? normalizedApiBaseUrl
).replace(/\/+$/, "");

export const HUBS_BASE_URL = normalizedApiBaseUrl.endsWith("/api")
  ? normalizedApiBaseUrl.slice(0, -4)
  : normalizedApiBaseUrl;
