import "server-only";
import { unstable_rethrow } from "next/navigation";
import { getAccessToken } from "@/lib/auth/session";
import { apiBaseUrl } from "@/lib/env";
import {
  clientProblem,
  networkProblem,
  toProblem,
  unauthenticatedErrorCode,
  type Problem,
} from "./problem";

export type ApiResult<T> = { ok: true; data: T } | { ok: false; problem: Problem };

export type QueryValues = Record<string, string | number | undefined>;

export type WriteMethod = "POST" | "PUT" | "PATCH" | "DELETE";

type RequestOptions = {
  method: "GET" | WriteMethod;
  query?: QueryValues;
  body?: unknown;
  token?: string;
  idempotencyKey?: string;
};

const timeoutMilliseconds = 15_000;

export function apiGet<T>(path: string, query?: QueryValues): Promise<ApiResult<T>> {
  return request<T>(path, { method: "GET", query });
}

export function apiPostAnonymous<T>(path: string, body: unknown): Promise<ApiResult<T>> {
  return request<T>(path, { method: "POST", body });
}

export async function apiWrite<T = undefined>(
  method: WriteMethod,
  path: string,
  options: { body?: unknown; idempotencyKey?: string } = {},
): Promise<ApiResult<T>> {
  const token = await getAccessToken();
  if (!token) return { ok: false, problem: clientProblem(401, unauthenticatedErrorCode) };

  return request<T>(path, { method, token, ...options });
}

async function request<T>(path: string, options: RequestOptions): Promise<ApiResult<T>> {
  const headers = new Headers({ Accept: "application/json" });
  if (options.body !== undefined) headers.set("Content-Type", "application/json");
  if (options.token) headers.set("Authorization", `Bearer ${options.token}`);
  if (options.idempotencyKey) headers.set("Idempotency-Key", options.idempotencyKey);

  let response: Response;
  let text: string;

  try {
    response = await fetch(buildUrl(path, options.query), {
      method: options.method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      cache: "no-store",
      signal: AbortSignal.timeout(timeoutMilliseconds),
    });
    text = await response.text();
  } catch (error) {
    unstable_rethrow(error);
    console.error(`La Api no respondió a ${options.method} ${path}`, error);
    return { ok: false, problem: networkProblem() };
  }

  const body = parseJson(text);
  if (!response.ok) {
    return {
      ok: false,
      problem: toProblem(response.status, body, response.headers.get("X-Trace-Id")),
    };
  }

  return { ok: true, data: body as T };
}

function buildUrl(path: string, query: QueryValues = {}): URL {
  const url = new URL(path, apiBaseUrl());

  for (const [name, value] of Object.entries(query)) {
    if (value !== undefined && value !== "") url.searchParams.set(name, String(value));
  }

  return url;
}

function parseJson(text: string): unknown {
  if (text === "") return undefined;

  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}
