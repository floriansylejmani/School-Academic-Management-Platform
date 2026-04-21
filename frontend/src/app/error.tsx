"use client";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

export default function Error({
  reset
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
      <Card className="max-w-xl p-8 text-center">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">System notice</p>
        <h1 className="mt-3 text-3xl font-semibold text-slate-950">Unable to load this view</h1>
        <p className="mt-4 text-sm leading-7 text-slate-500">
          Refresh the page or return to the previous section. If the problem continues, check the API connection and
          session status.
        </p>
        <Button className="mt-6" onClick={reset}>
          Try again
        </Button>
      </Card>
    </main>
  );
}
