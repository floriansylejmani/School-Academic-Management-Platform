"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { createQueryClient } from "@/lib/query-client";
import { RealtimeBridge } from "@/components/providers/realtime-bridge";
import { ToastViewport } from "@/components/ui/toast-viewport";

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => createQueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <RealtimeBridge />
      {children}
      <ToastViewport />
    </QueryClientProvider>
  );
}
