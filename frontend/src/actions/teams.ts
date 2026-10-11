"use server";

import { redirect } from "next/navigation";
import type { ActionState } from "@/lib/action-state";
import { apiWrite } from "@/lib/api/client";
import type { CreatedResponse } from "@/lib/api/types";
import { isGuid } from "@/lib/ids";
import { formInteger, formText, invalidRequest, settle } from "./support";

export async function createTeam(_state: ActionState, formData: FormData): Promise<ActionState> {
  const idempotencyKey = formText(formData, "idempotencyKey");
  const values = teamValues(formData);

  const result = await apiWrite<CreatedResponse>("POST", "/api/teams", {
    body: values,
    idempotencyKey,
  });

  return settle(result, {
    idempotencyKey,
    values,
    onSuccess: (created) => redirect(`/teams/${created.id}`),
  });
}

export async function updateTeam(
  teamId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(teamId)) return invalidRequest();

  const values = teamValues(formData);
  const result = await apiWrite("PUT", `/api/teams/${teamId}`, { body: values });

  return settle(result, { values });
}

export async function deleteTeam(teamId: string): Promise<ActionState> {
  if (!isGuid(teamId)) return invalidRequest();

  const result = await apiWrite("DELETE", `/api/teams/${teamId}`);

  return settle(result, { onSuccess: () => redirect("/teams") });
}

export async function addPlayer(
  teamId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(teamId)) return invalidRequest();

  const idempotencyKey = formText(formData, "idempotencyKey");
  const values = playerValues(formData);

  const result = await apiWrite<CreatedResponse>("POST", `/api/teams/${teamId}/players`, {
    body: { name: values.name, shirtNumber: formInteger(formData, "shirtNumber") },
    idempotencyKey,
  });

  return settle(result, { idempotencyKey, values });
}

export async function updatePlayer(
  teamId: string,
  playerId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  if (!isGuid(teamId) || !isGuid(playerId)) return invalidRequest();

  const values = playerValues(formData);
  const result = await apiWrite("PUT", `/api/teams/${teamId}/players/${playerId}`, {
    body: { name: values.name, shirtNumber: formInteger(formData, "shirtNumber") },
  });

  return settle(result, { values });
}

export async function removePlayer(teamId: string, playerId: string): Promise<ActionState> {
  if (!isGuid(teamId) || !isGuid(playerId)) return invalidRequest();

  const result = await apiWrite("DELETE", `/api/teams/${teamId}/players/${playerId}`);

  return settle(result);
}

function teamValues(formData: FormData) {
  return { name: formText(formData, "name"), country: formText(formData, "country") };
}

function playerValues(formData: FormData) {
  return { name: formText(formData, "name"), shirtNumber: formText(formData, "shirtNumber") };
}
