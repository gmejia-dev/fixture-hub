import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { AutoRefresh } from "@/components/auto-refresh";
import { SectionTitle } from "@/components/page-title";
import { ProblemAlert } from "@/components/problem-alert";
import { StatusBadge } from "@/components/status-badge";
import { card, link } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getMatch } from "@/lib/api/queries";
import { isGuid } from "@/lib/ids";

const liveRefreshSeconds = 10;

export async function generateMetadata({
  params,
}: PageProps<"/matches/[matchId]">): Promise<Metadata> {
  const { matchId } = await params;
  const match = isGuid(matchId) ? await getMatch(matchId) : null;
  const { t } = await getI18n();

  return {
    title: match?.ok
      ? `${match.data.homeTeamName} vs ${match.data.awayTeamName}`
      : t("matches.title"),
  };
}

export default async function MatchPage({ params }: PageProps<"/matches/[matchId]">) {
  const { matchId } = await params;
  if (!isGuid(matchId)) notFound();

  const [{ t, formatDateTime }, result] = await Promise.all([getI18n(), getMatch(matchId)]);

  if (!result.ok) {
    if (result.problem.status === 404) notFound();
    return <ProblemAlert problem={result.problem} />;
  }

  const match = result.data;
  const goals = [...match.goals].sort((a, b) => a.minute - b.minute);
  const teamName = (teamId: string) =>
    teamId === match.homeTeamId ? match.homeTeamName : match.awayTeamName;

  return (
    <div className="space-y-4">
      <Link href="/matches" className={`${link} text-sm`}>
        ← {t("matches.back")}
      </Link>

      <section className={`${card} p-6`}>
        <div className="flex flex-wrap items-center justify-center gap-3 text-sm text-slate-600">
          <StatusBadge status={match.status} label={t(`status.${match.status}`)} />
          <span>{formatDateTime(match.scheduledAt)}</span>
        </div>
        <div className="mt-4 grid grid-cols-[1fr_auto_1fr] items-center gap-4">
          <Link
            href={`/teams/${match.homeTeamId}`}
            className="text-right text-lg font-semibold hover:underline sm:text-2xl"
          >
            {match.homeTeamName}
          </Link>
          <span className="font-mono text-4xl font-bold tabular-nums sm:text-5xl">
            {match.status === "Scheduled" ? "– : –" : `${match.homeScore} : ${match.awayScore}`}
          </span>
          <Link
            href={`/teams/${match.awayTeamId}`}
            className="text-lg font-semibold hover:underline sm:text-2xl"
          >
            {match.awayTeamName}
          </Link>
        </div>
        {match.status === "InProgress" && (
          <div className="mt-4 flex justify-center">
            <AutoRefresh
              seconds={liveRefreshSeconds}
              label={t("live.autoRefresh", { seconds: liveRefreshSeconds })}
            />
          </div>
        )}
      </section>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <section className={`${card} p-4`}>
            <SectionTitle>{t("goals.title")}</SectionTitle>
            {goals.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">{t("goals.none")}</p>
            ) : (
              <ul className="mt-2 divide-y divide-slate-100">
                {goals.map((goal) => (
                  <li key={goal.id} className="flex flex-wrap items-center gap-3 py-2 text-sm">
                    <span className="w-10 font-mono font-semibold text-slate-500">
                      {goal.minute}&apos;
                    </span>
                    <span
                      className={goal.isAnnulled ? "text-slate-400 line-through" : "font-medium"}
                    >
                      {goal.playerName}
                    </span>
                    <span className="text-slate-500">
                      {t("goals.for", { team: teamName(goal.scoringTeamId) })}
                    </span>
                    {goal.isOwnGoal && (
                      <span className="rounded bg-amber-100 px-1.5 py-0.5 text-xs font-semibold text-amber-800">
                        {t("goals.ownGoal")}
                      </span>
                    )}
                    {goal.isAnnulled && (
                      <span className="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-semibold text-slate-600">
                        {t("goals.annulled")}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>

          {match.corrections.length > 0 && (
            <section className={`${card} p-4`}>
              <SectionTitle>{t("correction.history")}</SectionTitle>
              <ul className="mt-2 space-y-2 text-sm">
                {match.corrections.map((correction) => (
                  <li key={correction.id} className="rounded-md bg-slate-50 p-3">
                    <p className="font-mono font-semibold">
                      {correction.previousHomeScore} : {correction.previousAwayScore} →{" "}
                      {correction.newHomeScore} : {correction.newAwayScore}
                    </p>
                    <p className="mt-1 text-slate-600">{correction.reason}</p>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
