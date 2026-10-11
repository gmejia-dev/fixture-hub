import Link from "next/link";
import { AutoRefresh } from "@/components/auto-refresh";
import { MatchCards } from "@/components/match-list";
import { PageTitle, SectionTitle } from "@/components/page-title";
import { ProblemAlert } from "@/components/problem-alert";
import { card, cell, headerCell, link, row, table } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getMatches, getStandings, getTopScorers } from "@/lib/api/queries";

const liveRefreshSeconds = 15;

const summarySize = 5;

export default async function HomePage() {
  const top = { pageNumber: 1, pageSize: summarySize };

  const [{ t }, live, upcoming, standings, scorers] = await Promise.all([
    getI18n(),
    getMatches({ pageNumber: 1, pageSize: 10 }, { status: "InProgress" }),
    getMatches({ ...top, sortBy: "scheduledAt", sortDirection: "asc" }, { status: "Scheduled" }),
    getStandings(top),
    getTopScorers(top),
  ]);

  return (
    <div className="space-y-6">
      <PageTitle title={t("home.title")} subtitle={t("home.subtitle")} />

      <div className="grid gap-4 md:grid-cols-2">
        <section className={`${card} p-4`}>
          <div className="flex items-center justify-between gap-2">
            <SectionTitle>{t("home.live")}</SectionTitle>
            {live.ok && live.data.items.length > 0 && (
              <AutoRefresh
                seconds={liveRefreshSeconds}
                label={t("live.autoRefresh", { seconds: liveRefreshSeconds })}
              />
            )}
          </div>
          {live.ok ? (
            <MatchCards matches={live.data.items} empty={t("home.noLive")} />
          ) : (
            <ProblemAlert problem={live.problem} />
          )}
        </section>

        <section className={`${card} p-4`}>
          <div className="flex items-center justify-between gap-2">
            <SectionTitle>{t("home.upcoming")}</SectionTitle>
            <Link href="/matches?status=Scheduled" className={`${link} text-sm`}>
              {t("home.viewAll")}
            </Link>
          </div>
          {upcoming.ok ? (
            <MatchCards matches={upcoming.data.items} empty={t("home.noUpcoming")} />
          ) : (
            <ProblemAlert problem={upcoming.problem} />
          )}
        </section>

        <section className={`${card} p-4`}>
          <div className="flex items-center justify-between gap-2">
            <SectionTitle>{t("standings.title")}</SectionTitle>
            <Link href="/standings" className={`${link} text-sm`}>
              {t("home.viewAll")}
            </Link>
          </div>
          {standings.ok ? (
            <table className={`${table} mt-2`}>
              <thead>
                <tr>
                  <th className={headerCell}>#</th>
                  <th className={headerCell}>{t("standings.team")}</th>
                  <th className={`${headerCell} text-right`}>{t("standings.playedShort")}</th>
                  <th className={`${headerCell} text-right`}>{t("standings.pointsShort")}</th>
                </tr>
              </thead>
              <tbody>
                {standings.data.items.map((standing) => (
                  <tr key={standing.teamId} className={row}>
                    <td className={`${cell} text-slate-500`}>{standing.position}</td>
                    <td className={cell}>
                      <Link href={`/teams/${standing.teamId}`} className={link}>
                        {standing.teamName}
                      </Link>
                    </td>
                    <td className={`${cell} text-right`}>{standing.played}</td>
                    <td className={`${cell} text-right font-bold`}>{standing.points}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <ProblemAlert problem={standings.problem} />
          )}
        </section>

        <section className={`${card} p-4`}>
          <div className="flex items-center justify-between gap-2">
            <SectionTitle>{t("topScorers.title")}</SectionTitle>
            <Link href="/top-scorers" className={`${link} text-sm`}>
              {t("home.viewAll")}
            </Link>
          </div>
          {scorers.ok ? (
            scorers.data.items.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">{t("home.noScorers")}</p>
            ) : (
              <table className={`${table} mt-2`}>
                <thead>
                  <tr>
                    <th className={headerCell}>#</th>
                    <th className={headerCell}>{t("topScorers.player")}</th>
                    <th className={headerCell}>{t("topScorers.team")}</th>
                    <th className={`${headerCell} text-right`}>{t("topScorers.goals")}</th>
                  </tr>
                </thead>
                <tbody>
                  {scorers.data.items.map((scorer) => (
                    <tr key={scorer.playerId} className={row}>
                      <td className={`${cell} text-slate-500`}>{scorer.position}</td>
                      <td className={`${cell} font-medium`}>{scorer.playerName}</td>
                      <td className={`${cell} text-slate-600`}>{scorer.teamName}</td>
                      <td className={`${cell} text-right font-bold`}>{scorer.goals}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )
          ) : (
            <ProblemAlert problem={scorers.problem} />
          )}
        </section>
      </div>
    </div>
  );
}
