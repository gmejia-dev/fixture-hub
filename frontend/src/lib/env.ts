import "server-only";

export function apiBaseUrl(): string {
  return process.env.API_BASE_URL || "http://localhost:5065";
}

export function tournamentTimeZone(): string {
  return process.env.TOURNAMENT_TIME_ZONE || "America/El_Salvador";
}
