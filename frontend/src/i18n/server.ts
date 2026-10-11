import "server-only";
import { cookies, headers } from "next/headers";
import { cache } from "react";
import { formatDateTime } from "@/lib/dates";
import { tournamentTimeZone } from "@/lib/env";
import { catalogs } from "./catalogs";
import { localeCookieName } from "./config";
import { negotiateLocale } from "./negotiate";
import { createTranslator } from "./translate";

export const getI18n = cache(async () => {
  const [cookieStore, headerStore] = await Promise.all([cookies(), headers()]);
  const locale = negotiateLocale(
    cookieStore.get(localeCookieName)?.value,
    headerStore.get("accept-language"),
  );
  const messages = catalogs[locale];
  const timeZone = tournamentTimeZone();

  return {
    locale,
    messages,
    timeZone,
    t: createTranslator(messages),
    formatDateTime: (iso: string) => formatDateTime(iso, locale, timeZone),
  };
});
