"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";

export function AutoRefresh({ seconds, label }: { seconds: number; label: string }) {
  const router = useRouter();

  useEffect(() => {
    const id = window.setInterval(() => {
      if (document.visibilityState === "visible") router.refresh();
    }, seconds * 1000);

    return () => window.clearInterval(id);
  }, [router, seconds]);

  return (
    <p className="flex items-center gap-2 text-xs text-slate-500">
      <span className="h-2 w-2 animate-pulse rounded-full bg-red-500" aria-hidden="true" />
      {label}
    </p>
  );
}
