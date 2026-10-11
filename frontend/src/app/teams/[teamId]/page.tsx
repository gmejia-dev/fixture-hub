import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { deleteTeam } from "@/actions/teams";
import { FilterForm, TextFilter } from "@/components/filter-form";
import { ActionButton } from "@/components/forms/form-parts";
import { AddPlayerForm, EditTeamForm, PlayerRow } from "@/components/forms/team-forms";
import { EmptyRow, PageTitle, SectionTitle } from "@/components/page-title";
import { Pagination } from "@/components/pagination";
import { ProblemAlert } from "@/components/problem-alert";
import { SortHeader } from "@/components/sort-header";
import { card, headerCell, link, table, tableWrapper } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getPlayers, getTeam, playerSortFields } from "@/lib/api/queries";
import { getSession } from "@/lib/auth/session";
import { newIdempotencyKey } from "@/lib/idempotency";
import { isGuid } from "@/lib/ids";
import { listQuery, param, parseListParams } from "@/lib/paging";

export async function generateMetadata({
  params,
}: PageProps<"/teams/[teamId]">): Promise<Metadata> {
  const { teamId } = await params;
  const team = isGuid(teamId) ? await getTeam(teamId) : null;
  const { t } = await getI18n();

  return { title: team?.ok ? team.data.name : t("teams.title") };
}

export default async function TeamPage({ params, searchParams }: PageProps<"/teams/[teamId]">) {
  const { teamId } = await params;
  if (!isGuid(teamId)) notFound();

  const search = await searchParams;
  const list = parseListParams(search, playerSortFields);
  const name = param(search, "name");

  const [{ t }, team, players, session] = await Promise.all([
    getI18n(),
    getTeam(teamId),
    getPlayers(teamId, list, { name }),
    getSession(),
  ]);

  if (!team.ok) {
    if (team.problem.status === 404) notFound();
    return <ProblemAlert problem={team.problem} />;
  }

  const isAdmin = session?.isAdmin ?? false;
  const pathname = `/teams/${teamId}`;
  const paging = listQuery(list);
  const query = { ...paging, name };
  const header = { pathname, query, list, defaultField: "shirtNumber" };

  return (
    <div className="space-y-4">
      <Link href="/teams" className={`${link} text-sm`}>
        ← {t("teams.back")}
      </Link>

      <PageTitle title={team.data.name} subtitle={team.data.country}>
        <Link href={`/matches?teamId=${teamId}`} className={link}>
          {t("teams.viewMatches")}
        </Link>
      </PageTitle>

      <div className="grid gap-4 lg:grid-cols-3">
        <section className={`${card} lg:col-span-2`}>
          <div className="px-4 pt-4">
            <SectionTitle>{t("players.title", { count: team.data.players.length })}</SectionTitle>
          </div>

          <FilterForm
            pathname={pathname}
            keep={{
              sortBy: list.sortBy,
              sortDirection: list.sortDirection,
              pageSize: paging.pageSize,
            }}
            filters={{ name }}
          >
            <TextFilter name="name" label={t("filters.player")} value={name} />
          </FilterForm>

          {players.ok ? (
            <>
              <div className={tableWrapper}>
                <table className={table}>
                  <thead className="bg-slate-50">
                    <tr>
                      <SortHeader
                        {...header}
                        field="shirtNumber"
                        label={t("players.shirtNumber")}
                      />
                      <SortHeader {...header} field="name" label={t("players.name")} />
                      <th scope="col" className={headerCell}>
                        <span className="sr-only">{t("list.actions")}</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {players.data.items.length === 0 && (
                      <EmptyRow columns={3} message={t("list.empty")} />
                    )}
                    {players.data.items.map((player) => (
                      <PlayerRow
                        key={player.id}
                        teamId={teamId}
                        player={player}
                        isAdmin={isAdmin}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination pathname={pathname} query={query} page={players.data} />
            </>
          ) : (
            <div className="p-3">
              <ProblemAlert problem={players.problem} />
            </div>
          )}
        </section>

        {isAdmin && (
          <aside className="space-y-4">
            <section className={`${card} space-y-3 p-4`}>
              <SectionTitle>{t("players.new")}</SectionTitle>
              <AddPlayerForm teamId={teamId} idempotencyKey={newIdempotencyKey()} />
            </section>

            <section className={`${card} space-y-3 p-4`}>
              <SectionTitle>{t("teams.edit")}</SectionTitle>
              <EditTeamForm teamId={teamId} name={team.data.name} country={team.data.country} />
            </section>

            <section className={`${card} space-y-2 p-4`}>
              <SectionTitle>{t("teams.delete")}</SectionTitle>
              <p className="text-sm text-slate-600">{t("teams.deleteHint")}</p>
              <ActionButton
                action={deleteTeam.bind(null, teamId)}
                label={t("teams.delete")}
                pendingLabel={t("form.removing")}
                confirmMessage={t("teams.deleteConfirm", { name: team.data.name })}
              />
            </section>
          </aside>
        )}
      </div>
    </div>
  );
}
