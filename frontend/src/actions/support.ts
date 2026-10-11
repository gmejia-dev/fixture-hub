import "server-only";
import { refresh } from "next/cache";
import { headers } from "next/headers";
import { redirect } from "next/navigation";
import type { ActionState } from "@/lib/action-state";
import type { ApiResult } from "@/lib/api/client";
import {
  clientProblem,
  malformedRequestErrorCode,
  validationFailedErrorCode,
  type FieldErrors,
} from "@/lib/api/problem";
import { endSession } from "@/lib/auth/session";
import { newIdempotencyKey } from "@/lib/idempotency";
import { pathFromUrl } from "@/lib/return-to";

type SettleOptions<T> = {
  idempotencyKey?: string;
  values?: Record<string, string>;
  onSuccess?: (data: T) => void;
};

export function formText(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

export function formInteger(formData: FormData, name: string): number | null {
  const text = formText(formData, name);
  return /^-?\d+$/.test(text) ? Number(text) : null;
}

export function formFlag(formData: FormData, name: string): boolean {
  return formData.get(name) === "on";
}

export function invalidRequest(): ActionState {
  return { status: "error", problem: clientProblem(400, malformedRequestErrorCode) };
}

export function invalidFields(
  fieldErrors: FieldErrors,
  values: Record<string, string>,
  idempotencyKey?: string,
): ActionState {
  return {
    status: "error",
    problem: { ...clientProblem(400, validationFailedErrorCode), fieldErrors },
    values,
    idempotencyKey,
  };
}

export async function settle<T>(
  result: ApiResult<T>,
  options: SettleOptions<T> = {},
): Promise<ActionState> {
  if (result.ok) {
    options.onSuccess?.(result.data);
    refresh();

    return {
      status: "success",
      idempotencyKey: options.idempotencyKey === undefined ? undefined : newIdempotencyKey(),
    };
  }

  if (result.problem.status === 401) await signInAgain();

  return {
    status: "error",
    problem: result.problem,
    idempotencyKey: options.idempotencyKey,
    values: options.values,
  };
}

async function signInAgain(): Promise<never> {
  const headerStore = await headers();
  const returnTo = pathFromUrl(headerStore.get("referer"), headerStore.get("host"));

  await endSession();
  redirect(`/login?expired=1&returnTo=${encodeURIComponent(returnTo)}`);
}
