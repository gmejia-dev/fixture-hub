import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { annulGoal, cancelMatch, deleteMatch, finishMatch, startMatch } from "@/actions/matches";
import { AutoRefresh } from "@/components/auto-refresh";
import { ActionButton } from "@/components/forms/form-parts";
import {
  CorrectResultForm,
  RegisterGoalForm,
  RescheduleMatchForm,
  type TeamRoster,
} from "@/components/forms/match-forms";
import { SectionTitle } from "@/components/page-title";
import { ProblemAlert } from "@/components/problem-alert";
import { StatusBadge } from "@/components/status-badge";
import { card, link, primaryButton } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getMatch, getTeam } from "@/lib/api/queries";
import type { MatchDetails } from "@/lib/api/types";
import { getSession } from "@/lib/auth/session";
import { toDateTimeInput } from "@/lib/dates";
import { newIdempotencyKey } from "@/lib/idempotency";
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

  const [{ t, formatDateTime }, result, session] = await Promise.all([
    getI18n(),
    getMatch(matchId),
    getSession(),
  ]);

  if (!result.ok) {
    if (result.problem.status === 404) notFound();
    return <ProblemAlert problem={result.problem} />;
  }

  const match = result.data;
  const isAdmin = session?.isAdmin ?? false;
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
                    {isAdmin && match.status === "InProgress" && !goal.isAnnulled && (
                      <div className="ml-auto">
                        <ActionButton
                          action={annulGoal.bind(null, match.id, goal.id)}
                          label={t("goals.annul")}
                          pendingLabel={t("form.saving")}
                          confirmMessage={t("goals.annulConfirm", {
                            player: goal.playerName,
                            minute: goal.minute,
                          })}
                          idempotencyKey={newIdempotencyKey()}
                        />
                      </div>
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

        {isAdmin && <AdminPanel match={match} />}
      </div>
    </div>
  );
}

async function AdminPanel({ match }: { match: MatchDetails }) {
  const { t, timeZone } = await getI18n();
  const needsRosters = match.status === "InProgress" || match.status === "Finished";
  const rosters = needsRosters ? await getRosters(match) : [];

  return (
    <aside className="space-y-4">
      <section className={`${card} space-y-4 p-4`}>
        <SectionTitle>{t("admin.title")}</SectionTitle>

        {match.status === "Scheduled" && (
          <>
            <ActionButton
              action={startMatch.bind(null, match.id)}
              label={t("matches.start")}
              pendingLabel={t("form.saving")}
              idempotencyKey={newIdempotencyKey()}
              className={primaryButton}
            />
            <RescheduleMatchForm
              matchId={match.id}
              scheduledAt={toDateTimeInput(match.scheduledAt, timeZone)}
            />
          </>
        )}

        {match.status === "InProgress" && (
          <>
            <RegisterGoalForm
              matchId={match.id}
              teams={rosters}
              idempotencyKey={newIdempotencyKey()}
            />
            <hr className="border-slate-100" />
            <ActionButton
              action={finishMatch.bind(null, match.id)}
              label={t("matches.finish")}
              pendingLabel={t("form.saving")}
              confirmMessage={t("matches.finishConfirm")}
              idempotencyKey={newIdempotencyKey()}
              className={primaryButton}
            />
          </>
        )}

        {match.status === "Finished" && (
          <>
            <p className="text-sm text-slate-600">{t("correction.hint")}</p>
            <CorrectResultForm
              key={match.goals.map((goal) => `${goal.id}:${goal.isAnnulled}`).join("|")}
              matchId={match.id}
              teams={rosters}
              goals={match.goals
                .filter((goal) => !goal.isAnnulled)
                .map(({ playerId, minute, isOwnGoal }) => ({ playerId, minute, isOwnGoal }))}
            />
          </>
        )}

        {(match.status === "Scheduled" || match.status === "InProgress") && (
          <ActionButton
            action={cancelMatch.bind(null, match.id)}
            label={t("matches.cancel")}
            pendingLabel={t("form.saving")}
            confirmMessage={t("matches.cancelConfirm")}
            idempotencyKey={newIdempotencyKey()}
          />
        )}

        {(match.status === "Scheduled" || match.status === "Cancelled") && (
          <ActionButton
            action={deleteMatch.bind(null, match.id)}
            label={t("matches.delete")}
            pendingLabel={t("form.removing")}
            confirmMessage={t("matches.deleteConfirm")}
          />
        )}
      </section>
    </aside>
  );
}

async function getRosters(match: MatchDetails): Promise<TeamRoster[]> {
  const sides = [
    { id: match.homeTeamId, name: match.homeTeamName },
    { id: match.awayTeamId, name: match.awayTeamName },
  ];

  return Promise.all(
    sides.map(async (side) => {
      const team = await getTeam(side.id);
      return { ...side, players: team.ok ? team.data.players : [] };
    }),
  );
}
