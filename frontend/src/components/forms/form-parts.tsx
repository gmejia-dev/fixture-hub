"use client";

import { useActionState, type ReactNode } from "react";
import { useFormStatus } from "react-dom";
import { ProblemAlert } from "@/components/problem-alert";
import { dangerButton, label as labelStyle, primaryButton } from "@/components/styles";
import { idleState, type ActionState } from "@/lib/action-state";

export type FormAction = (state: ActionState, formData: FormData) => Promise<ActionState>;

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

export function IdempotencyKeyInput({ state, initial }: { state: ActionState; initial: string }) {
  return <input type="hidden" name="idempotencyKey" value={state.idempotencyKey ?? initial} />;
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

export function ActionButton({
  action,
  label,
  pendingLabel,
  confirmMessage,
  idempotencyKey,
  className = dangerButton,
}: {
  action: FormAction;
  label: string;
  pendingLabel: string;
  confirmMessage?: string;
  idempotencyKey?: string;
  className?: string;
}) {
  const [state, formAction] = useActionState(action, idleState);

  return (
    <form
      action={formAction}
      onSubmit={(event) => {
        if (confirmMessage && !window.confirm(confirmMessage)) event.preventDefault();
      }}
      className="space-y-2"
    >
      {idempotencyKey && <IdempotencyKeyInput state={state} initial={idempotencyKey} />}
      <SubmitButton pendingLabel={pendingLabel} className={className}>
        {label}
      </SubmitButton>
      <FormFeedback state={state} />
    </form>
  );
}
