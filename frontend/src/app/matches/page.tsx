import type { Metadata } from "next";
import Link from "next/link";
import { FilterForm, SelectFilter, TextFilter } from "@/components/filter-form";
import { ScheduleMatchForm } from "@/components/forms/match-forms";
import { Score } from "@/components/match-list";
import { EmptyRow, PageTitle, SectionTitle } from "@/components/page-title";
import { Pagination } from "@/components/pagination";
import { ProblemAlert } from "@/components/problem-alert";
import { SortHeader } from "@/components/sort-header";
import { StatusBadge } from "@/components/status-badge";
import { card, cell, link, row, table, tableWrapper } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getAllTeams, getMatches, matchSortFields } from "@/lib/api/queries";
import { matchStatuses, type MatchStatus } from "@/lib/api/types";
import { getSession } from "@/lib/auth/session";
import { endOfDayIso, isDateInput, startOfDayIso } from "@/lib/dates";
import { newIdempotencyKey } from "@/lib/idempotency";
import { isGuid } from "@/lib/ids";
import { listQuery, param, parseListParams } from "@/lib/paging";

const pathname = "/matches";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();
  return { title: t("matches.title") };
}

export default async function MatchesPage({ searchParams }: PageProps<"/matches">) {
  const search = await searchParams;
  const list = parseListParams(search, matchSortFields);
  const teamId = isGuid(param(search, "teamId")) ? param(search, "teamId") : undefined;
  const status = matchStatuses.find((s) => s === param(search, "status")) as
    MatchStatus | undefined;
  const from = isDateInput(param(search, "from")) ? param(search, "from") : undefined;
  const to = isDateInput(param(search, "to")) ? param(search, "to") : undefined;

  const { t, timeZone, formatDateTime } = await getI18n();
  const [result, teams, session] = await Promise.all([
    getMatches(list, {
      teamId,
      status,
      from: from ? (startOfDayIso(from, timeZone) ?? undefined) : undefined,
      to: to ? (endOfDayIso(to, timeZone) ?? undefined) : undefined,
    }),
    getAllTeams(),
    getSession(),
  ]);

  const paging = listQuery(list);
  const query = { ...paging, teamId, status, from, to };
  const header = { pathname, query, list, defaultField: "scheduledAt" };
  const teamOptions = teams.ok ? teams.data.data.map(({ id, name }) => ({ id, name })) : [];

  return (
    <div className="space-y-4">
      <PageTitle title={t("matches.title")} subtitle={t("matches.subtitle", { timeZone })} />

      {session?.isAdmin && (
        <section className={`${card} space-y-3 p-4`}>
          <SectionTitle>{t("matches.new")}</SectionTitle>
          <ScheduleMatchForm teams={teamOptions} idempotencyKey={newIdempotencyKey()} />
        </section>
      )}

      <section className={card}>
        <FilterForm
          pathname={pathname}
          keep={{
            sortBy: list.sortBy,
            sortDirection: list.sortDirection,
            pageSize: paging.pageSize,
          }}
          filters={{ teamId, status, from, to }}
        >
          <SelectFilter
            name="teamId"
            label={t("filters.team")}
            value={teamId}
            allLabel={t("filters.allTeams")}
            options={teamOptions.map((team) => ({ value: team.id, label: team.name }))}
          />
          <SelectFilter
            name="status"
            label={t("filters.status")}
            value={status}
            allLabel={t("filters.allStatuses")}
            options={matchStatuses.map((s) => ({ value: s, label: t(`status.${s}`) }))}
          />
          <TextFilter name="from" type="date" label={t("filters.from")} value={from} />
          <TextFilter name="to" type="date" label={t("filters.to")} value={to} />
        </FilterForm>

        {result.ok ? (
          <>
            <div className={tableWrapper}>
              <table className={table}>
                <thead className="bg-slate-50">
                  <tr>
                    <SortHeader {...header} field="scheduledAt" label={t("matches.scheduledAt")} />
                    <SortHeader
                      {...header}
                      field="homeTeam"
                      label={t("matches.home")}
                      align="right"
                    />
                    <th
                      scope="col"
                      className="px-3 py-2 text-center text-xs font-semibold uppercase tracking-wide text-slate-500"
                    >
                      {t("matches.score")}
                    </th>
                    <SortHeader {...header} field="awayTeam" label={t("matches.away")} />
                    <SortHeader {...header} field="status" label={t("matches.status")} />
                  </tr>
                </thead>
                <tbody>
                  {result.data.data.length === 0 && (
                    <EmptyRow columns={5} message={t("list.empty")} />
                  )}
                  {result.data.data.map((match) => (
                    <tr key={match.id} className={row}>
                      <td className={`${cell} whitespace-nowrap text-slate-600`}>
                        <Link href={`/matches/${match.id}`} className={link}>
                          {formatDateTime(match.scheduledAt)}
                        </Link>
                      </td>
                      <td className={`${cell} text-right font-medium`}>{match.homeTeamName}</td>
                      <td className={`${cell} text-center`}>
                        <Score match={match} />
                      </td>
                      <td className={`${cell} font-medium`}>{match.awayTeamName}</td>
                      <td className={cell}>
                        <StatusBadge status={match.status} label={t(`status.${match.status}`)} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination pathname={pathname} query={query} page={result.data} />
          </>
        ) : (
          <div className="p-3">
            <ProblemAlert problem={result.problem} />
          </div>
        )}
      </section>
    </div>
  );
}
