"use client";

import { useI18n } from "@/i18n/client";
import { describeProblem, type ProblemText } from "@/i18n/problems";
import type { Problem } from "@/lib/api/problem";

export function useProblemText(problem: Problem | undefined): ProblemText | null {
  const { t, formatDateTime } = useI18n();
  return problem ? describeProblem(t, problem, formatDateTime) : null;
}

export function ProblemAlert({
  problem,
  inlineFields = [],
}: {
  problem: Problem;
  inlineFields?: string[];
}) {
  const { t } = useI18n();
  const text = useProblemText(problem);
  if (!text) return null;

  const otherFields = Object.entries(text.fields)
    .filter(([field]) => !inlineFields.includes(field))
    .flatMap(([, messages]) => messages);

  return (
    <div
      role="alert"
      className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800"
    >
      <p className="font-medium">{text.message}</p>
      {otherFields.length > 0 && (
        <ul className="mt-1 list-disc pl-5">
          {otherFields.map((message, index) => (
            <li key={index}>{message}</li>
          ))}
        </ul>
      )}
      {text.traceId && (
        <p className="mt-2 text-xs text-red-700/80">
          {t("error.traceId")} <code className="select-all font-mono">{text.traceId}</code>
        </p>
      )}
    </div>
  );
}
