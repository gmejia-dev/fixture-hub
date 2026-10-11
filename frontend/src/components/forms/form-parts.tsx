"use client";

import type { ReactNode } from "react";
import { useFormStatus } from "react-dom";
import { ProblemAlert } from "@/components/problem-alert";
import { label as labelStyle, primaryButton } from "@/components/styles";
import type { ActionState } from "@/lib/action-state";

export function SubmitButton({
  children,
  pendingLabel,
  className = primaryButton,
}: {
  children: ReactNode;
  pendingLabel: string;
  className?: string;
}) {
  const { pending } = useFormStatus();

  return (
    <button type="submit" disabled={pending} aria-busy={pending} className={className}>
      {pending ? pendingLabel : children}
    </button>
  );
}

export function Field({
  label,
  errors,
  className = "",
  children,
}: {
  label: string;
  errors?: string[];
  className?: string;
  children: ReactNode;
}) {
  return (
    <label className={`block ${className}`}>
      <span className={labelStyle}>{label}</span>
      {children}
      {errors?.map((error) => (
        <span key={error} className="mt-1 block text-xs text-red-700">
          {error}
        </span>
      ))}
    </label>
  );
}

export function FormFeedback({
  state,
  inlineFields,
  successMessage,
}: {
  state: ActionState;
  inlineFields?: string[];
  successMessage?: string;
}) {
  if (state.status === "error" && state.problem) {
    return <ProblemAlert problem={state.problem} inlineFields={inlineFields} />;
  }

  if (state.status === "success" && successMessage) {
    return (
      <p role="status" className="text-sm font-medium text-emerald-700">
        {successMessage}
      </p>
    );
  }

  return null;
}
