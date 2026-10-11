"use client";

import { useActionState } from "react";
import { login } from "@/actions/auth";
import { useProblemText } from "@/components/problem-alert";
import { input } from "@/components/styles";
import { useI18n } from "@/i18n/client";
import { idleState } from "@/lib/action-state";
import { Field, FormFeedback, SubmitButton } from "./form-parts";

export function LoginForm({ returnTo }: { returnTo: string }) {
  const { t } = useI18n();
  const [state, action] = useActionState(login, idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="space-y-4">
      <input type="hidden" name="returnTo" value={returnTo} />
      <Field label={t("auth.username")} errors={problem?.fields.username}>
        <input
          name="username"
          autoComplete="username"
          required
          defaultValue={state.values?.username}
          className={input}
        />
      </Field>
      <Field label={t("auth.password")} errors={problem?.fields.password}>
        <input
          name="password"
          type="password"
          autoComplete="current-password"
          required
          className={input}
        />
      </Field>
      <FormFeedback state={state} inlineFields={["username", "password"]} />
      <SubmitButton pendingLabel={t("auth.signingIn")}>{t("auth.signIn")}</SubmitButton>
    </form>
  );
}
