import type { MatchStatus } from "@/lib/api/types";

const styles: Record<MatchStatus, string> = {
  Scheduled: "bg-slate-100 text-slate-700",
  InProgress: "bg-red-100 text-red-700",
  Finished: "bg-emerald-100 text-emerald-800",
  Cancelled: "bg-amber-100 text-amber-800",
};

export function StatusBadge({ status, label }: { status: MatchStatus; label: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ${styles[status]}`}
    >
      {status === "InProgress" && (
        <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-red-600" aria-hidden="true" />
      )}
      {label}
    </span>
  );
}
