import { cookies } from "next/headers";
import { SERVER_API_BASE_URL } from "@/services/apiConfig";
import type { AuthUser } from "@/types/auth";
import type { ApiResponse } from "@/types/common";

export async function getServerAuthSession(): Promise<AuthUser | null> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map(({ name, value }) => `${name}=${value}`)
    .join("; ");

  if (!cookieHeader) {
    return null;
  }

  let response: Response;
  try {
    response = await fetch(`${SERVER_API_BASE_URL}/auth/session`, {
      method: "GET",
      headers: {
        Cookie: cookieHeader
      },
      cache: "no-store"
    });
  } catch {
    return null;
  }

  if (!response.ok) {
    return null;
  }

  const payload = (await response.json()) as ApiResponse<AuthUser>;
  return payload.data;
}
