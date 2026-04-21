"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { LoadingScreen } from "@/components/ui/loading-screen";
import { useAuthStore } from "@/store/auth.store";
import type { AuthUser } from "@/types/auth";
import { getRoleDashboardPath, getRoleFromPath } from "@/utils/auth";

export function ProtectedRoute({
  children,
  initialUser
}: {
  children: React.ReactNode;
  initialUser?: AuthUser | null;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { clearSession, hasInitialized, hydrateSession, initializeSession, isAuthenticated, user } = useAuthStore();
  const resolvedUser = user ?? initialUser ?? null;
  const hasSessionContext = hasInitialized || Boolean(initialUser);

  useEffect(() => {
    if (initialUser) {
      initializeSession(initialUser);
      return;
    }

    if (!hasInitialized) {
      void hydrateSession()
        .then((sessionUser) => {
          if (!sessionUser) {
            router.replace("/login");
          }
        })
        .catch(() => {
          clearSession();
          router.replace("/login");
        });
      return;
    }

    if (!isAuthenticated || !user) {
      router.replace("/login");
      return;
    }

    const routeRole = getRoleFromPath(pathname);
    if (routeRole && routeRole !== user.role) {
      router.replace(getRoleDashboardPath(user.role));
    }
  }, [clearSession, hasInitialized, hydrateSession, initialUser, initializeSession, isAuthenticated, pathname, router, user]);

  if (!hasSessionContext) {
    return <LoadingScreen label="Restoring session..." />;
  }

  if (!resolvedUser) {
    return <LoadingScreen label="Redirecting to login..." />;
  }

  const routeRole = getRoleFromPath(pathname);
  if (routeRole && routeRole !== resolvedUser.role) {
    return <LoadingScreen label="Loading your dashboard..." />;
  }

  return <>{children}</>;
}
