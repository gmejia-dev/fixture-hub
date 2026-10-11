"use client";

import { useActionState, useState, type ChangeEvent } from "react";
import { correctResult, registerGoal, rescheduleMatch, scheduleMatch } from "@/actions/matches";
import { useProblemText } from "@/components/problem-alert";
import { dangerButton, input, secondaryButton } from "@/components/styles";
import { useI18n } from "@/i18n/client";
import { idleState } from "@/lib/action-state";
import type { PlayerItem } from "@/lib/api/types";
import { Field, FormFeedback, IdempotencyKeyInput, SubmitButton } from "./form-parts";

export type TeamOption = { id: string; name: string };

export type TeamRoster = TeamOption & { players: PlayerItem[] };

export type GoalDraft = { playerId: string; minute: number; isOwnGoal: boolean };

type GoalRow = { key: number; playerId: string; minute: string; isOwnGoal: boolean };

export function ScheduleMatchForm({
  teams,
  idempotencyKey,
}: {
  teams: TeamOption[];
  idempotencyKey: string;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(scheduleMatch, idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="grid gap-3 md:grid-cols-[1fr_1fr_14rem_auto] md:items-start">
      <IdempotencyKeyInput state={state} initial={idempotencyKey} />
      <Field label={t("matches.home")} errors={problem?.fields.homeTeamId}>
        <TeamSelect
          key={state.values?.homeTeamId}
          name="homeTeamId"
          teams={teams}
          defaultValue={state.values?.homeTeamId}
        />
      </Field>
      <Field label={t("matches.away")} errors={problem?.fields.awayTeamId}>
        <TeamSelect
          key={state.values?.awayTeamId}
          name="awayTeamId"
          teams={teams}
          defaultValue={state.values?.awayTeamId}
        />
      </Field>
      <Field label={t("matches.scheduledAt")} errors={problem?.fields.scheduledAt}>
        <input
          name="scheduledAt"
          type="datetime-local"
          required
          defaultValue={state.values?.scheduledAt}
          className={input}
        />
      </Field>
      <div className="md:pt-5">
        <SubmitButton pendingLabel={t("form.saving")}>{t("matches.schedule")}</SubmitButton>
      </div>
      <div className="md:col-span-4">
        <FormFeedback state={state} inlineFields={["homeTeamId", "awayTeamId", "scheduledAt"]} />
      </div>
    </form>
  );
}

export function RescheduleMatchForm({
  matchId,
  scheduledAt,
}: {
  matchId: string;
  scheduledAt: string;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(rescheduleMatch.bind(null, matchId), idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="space-y-3">
      <Field label={t("matches.scheduledAt")} errors={problem?.fields.scheduledAt}>
        <input
          name="scheduledAt"
          type="datetime-local"
          required
          defaultValue={state.values?.scheduledAt ?? scheduledAt}
          className={input}
        />
      </Field>
      <FormFeedback state={state} inlineFields={["scheduledAt"]} successMessage={t("form.saved")} />
      <SubmitButton pendingLabel={t("form.saving")} className={secondaryButton}>
        {t("matches.reschedule")}
      </SubmitButton>
    </form>
  );
}

export function RegisterGoalForm({
  matchId,
  teams,
  idempotencyKey,
}: {
  matchId: string;
  teams: TeamRoster[];
  idempotencyKey: string;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(registerGoal.bind(null, matchId), idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="space-y-3">
      <IdempotencyKeyInput state={state} initial={idempotencyKey} />
      <Field label={t("goals.player")} errors={problem?.fields.playerId}>
        <PlayerSelect
          key={state.values?.playerId}
          name="playerId"
          teams={teams}
          defaultValue={state.values?.playerId}
        />
      </Field>
      <div className="flex items-end gap-3">
        <Field label={t("goals.minute")} errors={problem?.fields.minute} className="w-28">
          <input
            name="minute"
            type="number"
            step={1}
            required
            defaultValue={state.values?.minute}
            className={input}
          />
        </Field>
        <label className="flex items-center gap-2 pb-2 text-sm">
          <input
            name="isOwnGoal"
            type="checkbox"
            defaultChecked={state.values?.isOwnGoal === "on"}
            className="h-4 w-4 accent-emerald-600"
          />
          {t("goals.ownGoal")}
        </label>
      </div>
      <FormFeedback
        state={state}
        inlineFields={["playerId", "minute"]}
        successMessage={t("goals.registered")}
      />
      <SubmitButton pendingLabel={t("form.saving")}>{t("goals.register")}</SubmitButton>
    </form>
  );
}

export function CorrectResultForm({
  matchId,
  teams,
  goals,
}: {
  matchId: string;
  teams: TeamRoster[];
  goals: GoalDraft[];
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(correctResult.bind(null, matchId), idleState);
  const problem = useProblemText(state.problem);
  const [rows, setRows] = useState<GoalRow[]>(() =>
    goals.map((goal, index) => ({ ...goal, key: index, minute: String(goal.minute) })),
  );
  const [nextKey, setNextKey] = useState(goals.length);

  const update = (key: number, change: Partial<GoalRow>) =>
    setRows((current) => current.map((r) => (r.key === key ? { ...r, ...change } : r)));

  const payload = rows.map(({ playerId, minute, isOwnGoal }) => ({
    playerId,
    minute: Number(minute),
    isOwnGoal,
  }));

  const inlineFields = [
    "reason",
    ...rows.flatMap((_, index) => [`goals[${index}].playerId`, `goals[${index}].minute`]),
  ];

  return (
    <form action={action} className="space-y-3">
      <input type="hidden" name="goals" value={JSON.stringify(payload)} />

      {rows.length === 0 && <p className="text-sm text-slate-500">{t("correction.noGoals")}</p>}

      <ul className="space-y-2">
        {rows.map((goalRow, index) => (
          <li
            key={goalRow.key}
            className="flex flex-wrap items-start gap-2 rounded-md bg-slate-50 p-2"
          >
            <Field
              label={t("goals.player")}
              errors={problem?.fields[`goals[${index}].playerId`]}
              className="min-w-48 flex-1"
            >
              <PlayerSelect
                teams={teams}
                value={goalRow.playerId}
                onChange={(playerId) => update(goalRow.key, { playerId })}
              />
            </Field>
            <Field
              label={t("goals.minute")}
              errors={problem?.fields[`goals[${index}].minute`]}
              className="w-24"
            >
              <input
                type="number"
                step={1}
                required
                value={goalRow.minute}
                onChange={(event) => update(goalRow.key, { minute: event.target.value })}
                className={input}
              />
            </Field>
            <label className="flex items-center gap-2 pt-6 text-sm">
              <input
                type="checkbox"
                checked={goalRow.isOwnGoal}
                onChange={(event) => update(goalRow.key, { isOwnGoal: event.target.checked })}
                className="h-4 w-4 accent-emerald-600"
              />
              {t("goals.ownGoal")}
            </label>
            <button
              type="button"
              onClick={() => setRows((current) => current.filter((r) => r.key !== goalRow.key))}
              className={`${dangerButton} mt-5`}
            >
              {t("correction.removeGoal")}
            </button>
          </li>
        ))}
      </ul>

      <button
        type="button"
        onClick={() => {
          setRows((current) => [
            ...current,
            { key: nextKey, playerId: "", minute: "", isOwnGoal: false },
          ]);
          setNextKey((key) => key + 1);
        }}
        className={secondaryButton}
      >
        {t("correction.addGoal")}
      </button>

      <Field label={t("correction.reason")} errors={problem?.fields.reason}>
        <textarea
          name="reason"
          rows={3}
          required
          defaultValue={state.values?.reason}
          className={input}
        />
      </Field>

      <FormFeedback
        state={state}
        inlineFields={inlineFields}
        successMessage={t("correction.saved")}
      />
      <SubmitButton pendingLabel={t("form.saving")}>{t("correction.submit")}</SubmitButton>
    </form>
  );
}

function TeamSelect({
  name,
  teams,
  defaultValue,
}: {
  name: string;
  teams: TeamOption[];
  defaultValue?: string;
}) {
  const { t } = useI18n();

  return (
    <select name={name} required defaultValue={defaultValue ?? ""} className={input}>
      <option value="" disabled>
        {t("form.choose")}
      </option>
      {teams.map((team) => (
        <option key={team.id} value={team.id}>
          {team.name}
        </option>
      ))}
    </select>
  );
}

function PlayerSelect({
  name,
  teams,
  defaultValue,
  value,
  onChange,
}: {
  name?: string;
  teams: TeamRoster[];
  defaultValue?: string;
  value?: string;
  onChange?: (playerId: string) => void;
}) {
  const { t } = useI18n();
  const controlled = onChange
    ? { value, onChange: (event: ChangeEvent<HTMLSelectElement>) => onChange(event.target.value) }
    : { defaultValue: defaultValue ?? "" };

  return (
    <select name={name} required className={input} {...controlled}>
      <option value="" disabled>
        {t("form.choose")}
      </option>
      {teams.map((team) => (
        <optgroup key={team.id} label={team.name}>
          {team.players.map((player) => (
            <option key={player.id} value={player.id}>
              {player.shirtNumber} · {player.name}
            </option>
          ))}
        </optgroup>
      ))}
    </select>
  );
}
