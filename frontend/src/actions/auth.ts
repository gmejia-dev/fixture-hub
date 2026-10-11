"use server";

import { redirect } from "next/navigation";
import type { ActionState } from "@/lib/action-state";
import { apiPostAnonymous } from "@/lib/api/client";
import type { AuthToken } from "@/lib/api/types";
import { endSession, startSession } from "@/lib/auth/session";
import { safeReturnTo } from "@/lib/return-to";
import { formText } from "./support";

export async function login(_state: ActionState, formData: FormData): Promise<ActionState> {
  const username = formText(formData, "username");
  const password = formData.get("password");

  const result = await apiPostAnonymous<AuthToken>("/api/auth/login", {
    username,
    password: typeof password === "string" ? password : "",
  });

  if (!result.ok) return { status: "error", problem: result.problem, values: { username } };

  await startSession(result.data);
  redirect(safeReturnTo(formText(formData, "returnTo")));
}

export async function logout(): Promise<void> {
  await endSession();
}
