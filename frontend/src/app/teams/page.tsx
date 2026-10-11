import type { Metadata } from "next";
import Link from "next/link";
import { FilterForm, TextFilter } from "@/components/filter-form";
import { CreateTeamForm } from "@/components/forms/team-forms";
import { EmptyRow, PageTitle, SectionTitle } from "@/components/page-title";
import { Pagination } from "@/components/pagination";
import { ProblemAlert } from "@/components/problem-alert";
import { SortHeader } from "@/components/sort-header";
import { card, cell, link, row, table, tableWrapper } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getTeams, teamSortFields } from "@/lib/api/queries";
import { getSession } from "@/lib/auth/session";
import { newIdempotencyKey } from "@/lib/idempotency";
import { listQuery, param, parseListParams } from "@/lib/paging";

const pathname = "/teams";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();
  return { title: t("teams.title") };
}

export default async function TeamsPage({ searchParams }: PageProps<"/teams">) {
  const search = await searchParams;
  const list = parseListParams(search, teamSortFields);
  const name = param(search, "name");
  const country = param(search, "country");

  const [{ t }, result, session] = await Promise.all([
    getI18n(),
    getTeams(list, { name, country }),
    getSession(),
  ]);

  const paging = listQuery(list);
  const query = { ...paging, name, country };
  const header = { pathname, query, list, defaultField: "name" };

  return (
    <div className="space-y-4">
      <PageTitle title={t("teams.title")} subtitle={t("teams.subtitle")} />

      {session?.isAdmin && (
        <section className={`${card} space-y-3 p-4`}>
          <SectionTitle>{t("teams.new")}</SectionTitle>
          <CreateTeamForm idempotencyKey={newIdempotencyKey()} />
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
          filters={{ name, country }}
        >
          <TextFilter name="name" label={t("filters.name")} value={name} />
          <TextFilter name="country" label={t("filters.country")} value={country} />
        </FilterForm>

        {result.ok ? (
          <>
            <div className={tableWrapper}>
              <table className={table}>
                <thead className="bg-slate-50">
                  <tr>
                    <SortHeader {...header} field="name" label={t("teams.name")} />
                    <SortHeader {...header} field="country" label={t("teams.country")} />
                    <SortHeader
                      {...header}
                      field="playerCount"
                      label={t("teams.players")}
                      align="right"
                      firstDirection="desc"
                    />
                  </tr>
                </thead>
                <tbody>
                  {result.data.data.length === 0 && (
                    <EmptyRow columns={3} message={t("list.empty")} />
                  )}
                  {result.data.data.map((team) => (
                    <tr key={team.id} className={row}>
                      <td className={cell}>
                        <Link href={`/teams/${team.id}`} className={link}>
                          {team.name}
                        </Link>
                      </td>
                      <td className={cell}>{team.country}</td>
                      <td className={`${cell} text-right`}>{team.playerCount}</td>
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
