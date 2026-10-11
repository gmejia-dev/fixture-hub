import type { Metadata } from "next";
import Link from "next/link";
import { FilterForm, TextFilter } from "@/components/filter-form";
import { EmptyRow, PageTitle } from "@/components/page-title";
import { Pagination } from "@/components/pagination";
import { ProblemAlert } from "@/components/problem-alert";
import { SortHeader } from "@/components/sort-header";
import { card, cell, link, row, table, tableWrapper } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getStandings, standingSortFields } from "@/lib/api/queries";
import { listQuery, param, parseListParams } from "@/lib/paging";

const pathname = "/standings";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();
  return { title: t("standings.title") };
}

export default async function StandingsPage({ searchParams }: PageProps<"/standings">) {
  const search = await searchParams;
  const list = parseListParams(search, standingSortFields);
  const teamName = param(search, "teamName");

  const [{ t }, result] = await Promise.all([getI18n(), getStandings(list, { teamName })]);

  const paging = listQuery(list);
  const query = { ...paging, teamName };
  const header = { pathname, query, list, defaultField: "position" };
  const numeric = { ...header, align: "right" as const, firstDirection: "desc" as const };

  return (
    <div className="space-y-4">
      <PageTitle title={t("standings.title")} subtitle={t("standings.subtitle")} />

      <section className={card}>
        <FilterForm
          pathname={pathname}
          keep={{
            sortBy: list.sortBy,
            sortDirection: list.sortDirection,
            pageSize: paging.pageSize,
          }}
          filters={{ teamName }}
        >
          <TextFilter name="teamName" label={t("filters.team")} value={teamName} />
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
                    <SortHeader {...header} field="teamName" label={t("standings.team")} />
                    <SortHeader
                      {...numeric}
                      field="played"
                      label={t("standings.playedShort")}
                      title={t("standings.played")}
                    />
                    <SortHeader
                      {...numeric}
                      field="won"
                      label={t("standings.wonShort")}
                      title={t("standings.won")}
                    />
                    <SortHeader
                      {...numeric}
                      field="drawn"
                      label={t("standings.drawnShort")}
                      title={t("standings.drawn")}
                    />
                    <SortHeader
                      {...numeric}
                      field="lost"
                      label={t("standings.lostShort")}
                      title={t("standings.lost")}
                    />
                    <SortHeader
                      {...numeric}
                      field="goalsFor"
                      label={t("standings.goalsForShort")}
                      title={t("standings.goalsFor")}
                    />
                    <SortHeader
                      {...numeric}
                      field="goalsAgainst"
                      label={t("standings.goalsAgainstShort")}
                      title={t("standings.goalsAgainst")}
                    />
                    <SortHeader
                      {...numeric}
                      field="goalDifference"
                      label={t("standings.goalDifferenceShort")}
                      title={t("standings.goalDifference")}
                    />
                    <SortHeader
                      {...numeric}
                      field="points"
                      label={t("standings.pointsShort")}
                      title={t("standings.points")}
                    />
                  </tr>
                </thead>
                <tbody>
                  {result.data.items.length === 0 && (
                    <EmptyRow columns={10} message={t("list.empty")} />
                  )}
                  {result.data.items.map((standing) => (
                    <tr key={standing.teamId} className={row}>
                      <td className={`${cell} font-semibold text-slate-500`}>
                        {standing.position}
                      </td>
                      <td className={cell}>
                        <Link href={`/teams/${standing.teamId}`} className={link}>
                          {standing.teamName}
                        </Link>
                      </td>
                      <td className={`${cell} text-right`}>{standing.played}</td>
                      <td className={`${cell} text-right`}>{standing.won}</td>
                      <td className={`${cell} text-right`}>{standing.drawn}</td>
                      <td className={`${cell} text-right`}>{standing.lost}</td>
                      <td className={`${cell} text-right`}>{standing.goalsFor}</td>
                      <td className={`${cell} text-right`}>{standing.goalsAgainst}</td>
                      <td className={`${cell} text-right`}>
                        {standing.goalDifference > 0
                          ? `+${standing.goalDifference}`
                          : standing.goalDifference}
                      </td>
                      <td className={`${cell} text-right text-base font-bold`}>
                        {standing.points}
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
