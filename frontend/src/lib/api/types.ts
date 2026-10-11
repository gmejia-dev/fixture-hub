export const matchStatuses = ["Scheduled", "InProgress", "Finished", "Cancelled"] as const;

export type MatchStatus = (typeof matchStatuses)[number];

export type PagedResult<TItem> = {
  items: TItem[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
};

export type TeamSummary = {
  id: string;
  name: string;
  country: string;
  playerCount: number;
};

export type PlayerItem = {
  id: string;
  name: string;
  shirtNumber: number;
};

export type TeamDetails = {
  id: string;
  name: string;
  country: string;
  players: PlayerItem[];
};

export type MatchSummary = {
  id: string;
  homeTeamId: string;
  homeTeamName: string;
  awayTeamId: string;
  awayTeamName: string;
  scheduledAt: string;
  status: MatchStatus;
  homeScore: number;
  awayScore: number;
};

export type GoalItem = {
  id: string;
  playerId: string;
  playerName: string;
  scoringTeamId: string;
  minute: number;
  isOwnGoal: boolean;
  isAnnulled: boolean;
};

export type CorrectionItem = {
  id: string;
  previousHomeScore: number;
  previousAwayScore: number;
  newHomeScore: number;
  newAwayScore: number;
  reason: string;
};

export type MatchDetails = MatchSummary & {
  goals: GoalItem[];
  corrections: CorrectionItem[];
};

export type StandingItem = {
  position: number;
  teamId: string;
  teamName: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
};

export type TopScorerItem = {
  position: number;
  playerId: string;
  playerName: string;
  teamId: string;
  teamName: string;
  goals: number;
};

export type AuthToken = {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
};

export type CreatedResponse = {
  id: string;
};
