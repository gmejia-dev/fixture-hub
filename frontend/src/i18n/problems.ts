import type { Problem } from "@/lib/api/problem";
import type { MessageValues, Translator } from "./translate";

export type ProblemText = {
  message: string;
  fields: Record<string, string[]>;
  traceId?: string;
};

const statusMetadata = new Set(["currentStatus", "targetStatus"]);

const dateMetadata = new Set(["scheduledAt"]);

export function describeProblem(
  t: Translator,
  problem: Problem,
  formatDateTime: (iso: string) => string,
): ProblemText {
  const fields = Object.fromEntries(
    Object.entries(problem.fieldErrors ?? {}).map(([field, codes]) => [
      field,
      codes.map((code) => errorMessage(t, code, {}, formatDateTime)),
    ]),
  );

  return {
    message: errorMessage(t, problem.errorCode, problem.metadata, formatDateTime),
    fields,
    traceId: problem.traceId,
  };
}

export function errorMessage(
  t: Translator,
  code: string,
  metadata: Record<string, unknown>,
  formatDateTime: (iso: string) => string,
): string {
  const key = `error.${code}`;
  if (!t.has(key)) return t("error.unknown");

  return t(key, metadataValues(t, metadata, formatDateTime));
}

function metadataValues(
  t: Translator,
  metadata: Record<string, unknown>,
  formatDateTime: (iso: string) => string,
): MessageValues {
  const values: MessageValues = {};

  for (const [name, value] of Object.entries(metadata)) {
    if (typeof value === "number") values[name] = value;
    if (typeof value !== "string") continue;

    if (statusMetadata.has(name)) values[name] = statusLabel(t, value);
    else if (dateMetadata.has(name)) values[name] = formatDateTime(value);
    else values[name] = value;
  }

  return values;
}

function statusLabel(t: Translator, status: string): string {
  const key = `status.${status}`;
  return t.has(key) ? t(key) : status;
}
