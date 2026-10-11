import type { Metadata } from "next";
import Link from "next/link";
import { FilterForm, SelectFilter, TextFilter } from "@/components/filter-form";
import { EmptyRow, PageTitle } from "@/components/page-title";
import { Pagination } from "@/components/pagination";
import { ProblemAlert } from "@/components/problem-alert";
import { SortHeader } from "@/components/sort-header";
import { card, cell, link, row, table, tableWrapper } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getAllTeams, getTopScorers, topScorerSortFields } from "@/lib/api/queries";
import { isGuid } from "@/lib/ids";
import { listQuery, param, parseListParams } from "@/lib/paging";

const pathname = "/top-scorers";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();
  return { title: t("topScorers.title") };
}

export default async function TopScorersPage({ searchParams }: PageProps<"/top-scorers">) {
  const search = await searchParams;
  const list = parseListParams(search, topScorerSortFields);
  const teamId = isGuid(param(search, "teamId")) ? param(search, "teamId") : undefined;
  const playerName = param(search, "playerName");

  const [{ t }, result, teams] = await Promise.all([
    getI18n(),
    getTopScorers(list, { teamId, playerName }),
    getAllTeams(),
  ]);

  const paging = listQuery(list);
  const query = { ...paging, teamId, playerName };
  const header = { pathname, query, list, defaultField: "position" };

  return (
    <div className="space-y-4">
      <PageTitle title={t("topScorers.title")} subtitle={t("topScorers.subtitle")} />

      <section className={card}>
        <FilterForm
          pathname={pathname}
          keep={{
            sortBy: list.sortBy,
            sortDirection: list.sortDirection,
            pageSize: paging.pageSize,
          }}
          filters={{ teamId, playerName }}
        >
          <SelectFilter
            name="teamId"
            label={t("filters.team")}
            value={teamId}
            allLabel={t("filters.allTeams")}
            options={
              teams.ok ? teams.data.items.map((team) => ({ value: team.id, label: team.name })) : []
            }
          />
          <TextFilter name="playerName" label={t("filters.player")} value={playerName} />
        </FilterForm>

        {result.ok ? (
          <>
            <div className={tableWrapper}>
              <table className={table}>
                <thead className="bg-slate-50">
                  <tr>
                    <SortHeader
                      {...header}
                      field="position"
                      label="#"
                      title={t("standings.position")}
                    />
                    <SortHeader {...header} field="playerName" label={t("topScorers.player")} />
                    <SortHeader {...header} field="teamName" label={t("topScorers.team")} />
                    <SortHeader
                      {...header}
                      field="goals"
                      label={t("topScorers.goals")}
                      align="right"
                      firstDirection="desc"
                    />
                  </tr>
                </thead>
                <tbody>
                  {result.data.items.length === 0 && (
                    <EmptyRow columns={4} message={t("list.empty")} />
                  )}
                  {result.data.items.map((scorer) => (
                    <tr key={scorer.playerId} className={row}>
                      <td className={`${cell} font-semibold text-slate-500`}>{scorer.position}</td>
                      <td className={`${cell} font-medium`}>{scorer.playerName}</td>
                      <td className={cell}>
                        <Link href={`/teams/${scorer.teamId}`} className={link}>
                          {scorer.teamName}
                        </Link>
                      </td>
                      <td className={`${cell} text-right text-base font-bold`}>{scorer.goals}</td>
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
