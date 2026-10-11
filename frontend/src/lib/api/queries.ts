import "server-only";
import { cache } from "react";
import type { ListParams } from "@/lib/paging";
import { apiGet, type ApiResult } from "./client";
import type {
  MatchDetails,
  MatchStatus,
  MatchSummary,
  PagedResult,
  PlayerItem,
  StandingItem,
  TeamDetails,
  TeamSummary,
  TopScorerItem,
} from "./types";

export const teamSortFields = ["name", "country", "playerCount"] as const;

export const playerSortFields = ["shirtNumber", "name"] as const;

export const matchSortFields = ["scheduledAt", "homeTeam", "awayTeam", "status"] as const;

export const standingSortFields = [
  "position",
  "teamName",
  "played",
  "won",
  "drawn",
  "lost",
  "goalsFor",
  "goalsAgainst",
  "goalDifference",
  "points",
] as const;

export const topScorerSortFields = ["position", "goals", "playerName", "teamName"] as const;

export const maxPageSize = 100;

export function getTeams(
  list: ListParams,
  filters: { name?: string; country?: string } = {},
): Promise<ApiResult<PagedResult<TeamSummary>>> {
  return apiGet("/api/teams", { ...list, ...filters });
}

export function getAllTeams(): Promise<ApiResult<PagedResult<TeamSummary>>> {
  return getTeams({ pageNumber: 1, pageSize: maxPageSize, sortBy: "name" });
}

export const getTeam = cache((teamId: string): Promise<ApiResult<TeamDetails>> =>
  apiGet(`/api/teams/${teamId}`),
);

export function getPlayers(
  teamId: string,
  list: ListParams,
  filters: { name?: string } = {},
): Promise<ApiResult<PagedResult<PlayerItem>>> {
  return apiGet(`/api/teams/${teamId}/players`, { ...list, ...filters });
}

export function getMatches(
  list: ListParams,
  filters: { teamId?: string; status?: MatchStatus; from?: string; to?: string } = {},
): Promise<ApiResult<PagedResult<MatchSummary>>> {
  return apiGet("/api/matches", { ...list, ...filters });
}

export const getMatch = cache((matchId: string): Promise<ApiResult<MatchDetails>> =>
  apiGet(`/api/matches/${matchId}`),
);

export function getStandings(
  list: ListParams,
  filters: { teamName?: string } = {},
): Promise<ApiResult<PagedResult<StandingItem>>> {
  return apiGet("/api/standings", { ...list, ...filters });
}

export function getTopScorers(
  list: ListParams,
  filters: { teamId?: string; playerName?: string } = {},
): Promise<ApiResult<PagedResult<TopScorerItem>>> {
  return apiGet("/api/top-scorers", { ...list, ...filters });
}
