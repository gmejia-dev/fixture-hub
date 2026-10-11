"use client";

import { useActionState, useState } from "react";
import { addPlayer, createTeam, removePlayer, updatePlayer, updateTeam } from "@/actions/teams";
import { useProblemText } from "@/components/problem-alert";
import { cell, input, row, secondaryButton } from "@/components/styles";
import { useI18n } from "@/i18n/client";
import { idleState, type ActionState } from "@/lib/action-state";
import type { PlayerItem } from "@/lib/api/types";
import { ActionButton, Field, FormFeedback, IdempotencyKeyInput, SubmitButton } from "./form-parts";

const teamFields = ["name", "country"];

const playerFields = ["name", "shirtNumber"];

export function CreateTeamForm({ idempotencyKey }: { idempotencyKey: string }) {
  const { t } = useI18n();
  const [state, action] = useActionState(createTeam, idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="grid gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-start">
      <IdempotencyKeyInput state={state} initial={idempotencyKey} />
      <Field label={t("teams.name")} errors={problem?.fields.name}>
        <input name="name" required defaultValue={state.values?.name} className={input} />
      </Field>
      <Field label={t("teams.country")} errors={problem?.fields.country}>
        <input name="country" required defaultValue={state.values?.country} className={input} />
      </Field>
      <div className="sm:pt-5">
        <SubmitButton pendingLabel={t("form.saving")}>{t("teams.create")}</SubmitButton>
      </div>
      <div className="sm:col-span-3">
        <FormFeedback state={state} inlineFields={teamFields} />
      </div>
    </form>
  );
}

export function EditTeamForm({
  teamId,
  name,
  country,
}: {
  teamId: string;
  name: string;
  country: string;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(updateTeam.bind(null, teamId), idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="space-y-3">
      <Field label={t("teams.name")} errors={problem?.fields.name}>
        <input name="name" required defaultValue={state.values?.name ?? name} className={input} />
      </Field>
      <Field label={t("teams.country")} errors={problem?.fields.country}>
        <input
          name="country"
          required
          defaultValue={state.values?.country ?? country}
          className={input}
        />
      </Field>
      <FormFeedback state={state} inlineFields={teamFields} successMessage={t("form.saved")} />
      <SubmitButton pendingLabel={t("form.saving")}>{t("form.save")}</SubmitButton>
    </form>
  );
}

export function AddPlayerForm({
  teamId,
  idempotencyKey,
}: {
  teamId: string;
  idempotencyKey: string;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(addPlayer.bind(null, teamId), idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="space-y-3">
      <IdempotencyKeyInput state={state} initial={idempotencyKey} />
      <div className="grid grid-cols-[1fr_6rem] gap-3">
        <Field label={t("players.name")} errors={problem?.fields.name}>
          <input name="name" required defaultValue={state.values?.name} className={input} />
        </Field>
        <Field label={t("players.shirtNumber")} errors={problem?.fields.shirtNumber}>
          <input
            name="shirtNumber"
            type="number"
            step={1}
            required
            defaultValue={state.values?.shirtNumber}
            className={input}
          />
        </Field>
      </div>
      <FormFeedback state={state} inlineFields={playerFields} successMessage={t("players.added")} />
      <SubmitButton pendingLabel={t("form.saving")}>{t("players.add")}</SubmitButton>
    </form>
  );
}

export function PlayerRow({
  teamId,
  player,
  isAdmin,
}: {
  teamId: string;
  player: PlayerItem;
  isAdmin: boolean;
}) {
  const { t } = useI18n();
  const [editing, setEditing] = useState(false);

  if (editing) {
    return (
      <tr className={row}>
        <td colSpan={3} className={cell}>
          <EditPlayerForm teamId={teamId} player={player} onClose={() => setEditing(false)} />
        </td>
      </tr>
    );
  }

  return (
    <tr className={row}>
      <td className={`${cell} w-20 font-mono font-semibold text-slate-500`}>
        {player.shirtNumber}
      </td>
      <td className={`${cell} font-medium`}>{player.name}</td>
      <td className={cell}>
        {isAdmin && (
          <div className="flex flex-wrap justify-end gap-2">
            <button type="button" onClick={() => setEditing(true)} className={secondaryButton}>
              {t("form.edit")}
            </button>
            <ActionButton
              action={removePlayer.bind(null, teamId, player.id)}
              label={t("players.remove")}
              pendingLabel={t("form.removing")}
              confirmMessage={t("players.removeConfirm", { name: player.name })}
            />
          </div>
        )}
      </td>
    </tr>
  );
}

function EditPlayerForm({
  teamId,
  player,
  onClose,
}: {
  teamId: string;
  player: PlayerItem;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const [state, action] = useActionState(async (previous: ActionState, formData: FormData) => {
    const next = await updatePlayer(teamId, player.id, previous, formData);
    if (next.status === "success") onClose();
    return next;
  }, idleState);
  const problem = useProblemText(state.problem);

  return (
    <form action={action} className="grid gap-3 sm:grid-cols-[8rem_1fr_auto] sm:items-start">
      <Field label={t("players.shirtNumber")} errors={problem?.fields.shirtNumber}>
        <input
          name="shirtNumber"
          type="number"
          step={1}
          required
          defaultValue={state.values?.shirtNumber ?? player.shirtNumber}
          className={input}
        />
      </Field>
      <Field label={t("players.name")} errors={problem?.fields.name}>
        <input
          name="name"
          required
          defaultValue={state.values?.name ?? player.name}
          className={input}
        />
      </Field>
      <div className="flex gap-2 sm:pt-5">
        <SubmitButton pendingLabel={t("form.saving")}>{t("form.save")}</SubmitButton>
        <button type="button" onClick={onClose} className={secondaryButton}>
          {t("form.cancel")}
        </button>
      </div>
      <div className="sm:col-span-3">
        <FormFeedback state={state} inlineFields={playerFields} />
      </div>
    </form>
  );
}
