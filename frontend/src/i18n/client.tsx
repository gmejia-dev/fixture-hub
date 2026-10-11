"use client";

import { createContext, use, useMemo, type ReactNode } from "react";
import { formatDateTime } from "@/lib/dates";
import type { Messages } from "./catalogs";
import type { Locale } from "./config";
import { createTranslator, type Translator } from "./translate";

type I18n = {
  locale: Locale;
  t: Translator;
  formatDateTime: (iso: string) => string;
};

const I18nContext = createContext<I18n | null>(null);

export function I18nProvider({
  locale,
  messages,
  timeZone,
  children,
}: {
  locale: Locale;
  messages: Messages;
  timeZone: string;
  children: ReactNode;
}) {
  const value = useMemo(
    () => ({
      locale,
      t: createTranslator(messages),
      formatDateTime: (iso: string) => formatDateTime(iso, locale, timeZone),
    }),
    [locale, messages, timeZone],
  );

  return <I18nContext value={value}>{children}</I18nContext>;
}

export function useI18n(): I18n {
  const i18n = use(I18nContext);
  if (!i18n) throw new Error("useI18n necesita un I18nProvider más arriba en el árbol.");

  return i18n;
}
