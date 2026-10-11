import Link from "next/link";
import { getI18n } from "@/i18n/server";
import type { MatchSummary } from "@/lib/api/types";
import { StatusBadge } from "./status-badge";

export function Score({ match }: { match: MatchSummary }) {
  if (match.status === "Scheduled") return <span className="text-slate-400">vs</span>;

  return (
    <span className="font-mono text-base font-bold tabular-nums">
      {match.homeScore} – {match.awayScore}
    </span>
  );
}

export async function MatchCards({ matches, empty }: { matches: MatchSummary[]; empty: string }) {
  const { t, formatDateTime } = await getI18n();

  if (matches.length === 0) return <p className="text-sm text-slate-500">{empty}</p>;

  return (
    <ul className="divide-y divide-slate-100">
      {matches.map((match) => (
        <li key={match.id}>
          <Link
            href={`/matches/${match.id}`}
            className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 rounded-md px-2 py-3 hover:bg-slate-50"
          >
            <span className="truncate text-right font-medium">{match.homeTeamName}</span>
            <span className="flex flex-col items-center gap-1">
              <Score match={match} />
              <StatusBadge status={match.status} label={t(`status.${match.status}`)} />
            </span>
            <span className="truncate font-medium">{match.awayTeamName}</span>
            <span className="col-span-3 text-center text-xs text-slate-500">
              {formatDateTime(match.scheduledAt)}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  );
}
