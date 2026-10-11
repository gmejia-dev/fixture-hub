"use server";

import { redirect } from "next/navigation";
import type { ActionState } from "@/lib/action-state";
import { apiWrite } from "@/lib/api/client";
import type { CreatedResponse } from "@/lib/api/types";
import { dateTimeInputToIso } from "@/lib/dates";
import { tournamentTimeZone } from "@/lib/env";
import { isGuid } from "@/lib/ids";
import { formFlag, formInteger, formText, invalidFields, invalidRequest, settle } from "./support";

type CorrectedGoal = { playerId: string; minute: number; isOwnGoal: boolean };

export async function scheduleMatch(_state: ActionState, formData: FormData): Promise<ActionState> {
  const idempotencyKey = formText(formData, "idempotencyKey");
  const values = {
    homeTeamId: formText(formData, "homeTeamId"),
    awayTeamId: formText(formData, "awayTeamId"),
    scheduledAt: formText(formData, "scheduledAt"),
  };

  const scheduledAt = dateTimeInputToIso(values.scheduledAt, tournamentTimeZone());
  if (!scheduledAt) {
    return invalidFields({ scheduledAt: ["Match.ScheduledAtRequired"] }, values, idempotencyKey);
  }

  const result = await apiWrite<CreatedResponse>("POST", "/api/matches", {
    body: {
      homeTeamId: values.homeTeamId || null,
      awayTeamId: values.awayTeamId || null,
      scheduledAt,
    },
    idempotencyKey,
  });

  return settle(result, {
    idempotencyKey,
    values,
    onSuccess: (created) => redirect(`/matches/${created.id}`),
  });
}

export async function rescheduleMatch(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(matchId)) return invalidRequest();

  const values = { scheduledAt: formText(formData, "scheduledAt") };
  const scheduledAt = dateTimeInputToIso(values.scheduledAt, tournamentTimeZone());
  if (!scheduledAt) return invalidFields({ scheduledAt: ["Match.ScheduledAtRequired"] }, values);

  const result = await apiWrite("PATCH", `/api/matches/${matchId}`, { body: { scheduledAt } });

  return settle(result, { values });
}

export async function deleteMatch(matchId: string): Promise<ActionState> {
  if (!isGuid(matchId)) return invalidRequest();

  const result = await apiWrite("DELETE", `/api/matches/${matchId}`);

  return settle(result, { onSuccess: () => redirect("/matches") });
}

export async function startMatch(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  return changeStatus(matchId, "start", formData);
}

export async function finishMatch(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  return changeStatus(matchId, "finish", formData);
}

export async function cancelMatch(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  return changeStatus(matchId, "cancel", formData);
}

export async function registerGoal(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(matchId)) return invalidRequest();

  const idempotencyKey = formText(formData, "idempotencyKey");
  const values = {
    playerId: formText(formData, "playerId"),
    minute: formText(formData, "minute"),
    isOwnGoal: formFlag(formData, "isOwnGoal") ? "on" : "",
  };

  const result = await apiWrite<CreatedResponse>("POST", `/api/matches/${matchId}/goals`, {
    body: {
      playerId: values.playerId || null,
      minute: formInteger(formData, "minute"),
      isOwnGoal: values.isOwnGoal === "on",
    },
    idempotencyKey,
  });

  return settle(result, { idempotencyKey, values });
}

export async function annulGoal(
  matchId: string,
  goalId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(matchId) || !isGuid(goalId)) return invalidRequest();

  const idempotencyKey = formText(formData, "idempotencyKey");
  const result = await apiWrite("POST", `/api/matches/${matchId}/goals/${goalId}/annul`, {
    idempotencyKey,
  });

  return settle(result, { idempotencyKey });
}

export async function correctResult(
  matchId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(matchId)) return invalidRequest();

  const values = { goals: formText(formData, "goals"), reason: formText(formData, "reason") };
  const goals = parseGoals(values.goals);
  if (!goals) return invalidRequest();

  const result = await apiWrite("PUT", `/api/matches/${matchId}/result`, {
    body: { goals, reason: values.reason },
  });

  return settle(result, { values });
}

async function changeStatus(
  matchId: string,
  transition: "start" | "finish" | "cancel",
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(matchId)) return invalidRequest();

  const idempotencyKey = formText(formData, "idempotencyKey");
  const result = await apiWrite("POST", `/api/matches/${matchId}/${transition}`, {
    idempotencyKey,
  });

  return settle(result, { idempotencyKey });
}

function parseGoals(json: string): CorrectedGoal[] | null {
  try {
    const goals: unknown = JSON.parse(json);
    if (!Array.isArray(goals)) return null;

    return goals.every(isCorrectedGoal)
      ? goals.map(({ playerId, minute, isOwnGoal }) => ({ playerId, minute, isOwnGoal }))
      : null;
  } catch {
    return null;
  }
}

function isCorrectedGoal(value: unknown): value is CorrectedGoal {
  if (typeof value !== "object" || value === null) return false;

  const { playerId, minute, isOwnGoal } = value as Record<string, unknown>;
  return isGuid(playerId) && Number.isInteger(minute) && typeof isOwnGoal === "boolean";
}
