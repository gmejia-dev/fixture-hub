import Form from "next/form";
import Link from "next/link";
import type { ReactNode } from "react";
import { getI18n } from "@/i18n/server";
import { hrefWith, type QueryParams } from "@/lib/paging";
import { input, label as labelStyle, primaryButton, secondaryButton } from "./styles";

export async function FilterForm({
  pathname,
  keep,
  filters,
  children,
}: {
  pathname: string;
  keep: QueryParams;
  filters: QueryParams;
  children: ReactNode;
}) {
  const { t } = await getI18n();

  return (
    <Form
      key={JSON.stringify(filters)}
      action={pathname}
      className="flex flex-wrap items-end gap-3 p-3"
    >
      {Object.entries(keep).map(([name, value]) =>
        value === undefined ? null : <input key={name} type="hidden" name={name} value={value} />,
      )}
      {children}
      <div className="flex gap-2">
        <button type="submit" className={primaryButton}>
          {t("filters.apply")}
        </button>
        <Link href={hrefWith(pathname, keep)} className={secondaryButton}>
          {t("filters.clear")}
        </Link>
      </div>
    </Form>
  );
}

export function TextFilter({
  name,
  label,
  value,
  type = "search",
}: {
  name: string;
  label: string;
  value?: string;
  type?: "search" | "date";
}) {
  return (
    <label className="block min-w-40 flex-1">
      <span className={labelStyle}>{label}</span>
      <input type={type} name={name} defaultValue={value} className={input} />
    </label>
  );
}

export function SelectFilter({
  name,
  label,
  value,
  allLabel,
  options,
}: {
  name: string;
  label: string;
  value?: string;
  allLabel: string;
  options: { value: string; label: string }[];
}) {
  return (
    <label className="block min-w-40 flex-1">
      <span className={labelStyle}>{label}</span>
      <select name={name} defaultValue={value ?? ""} className={input}>
        <option value="">{allLabel}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
