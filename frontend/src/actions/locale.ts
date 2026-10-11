"use server";

import { cookies } from "next/headers";
import { isLocale, localeCookieName } from "@/i18n/config";

const oneYearInSeconds = 60 * 60 * 24 * 365;

export async function changeLocale(formData: FormData): Promise<void> {
  const locale = formData.get("locale");
  if (!isLocale(locale)) return;

  (await cookies()).set(localeCookieName, locale, {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: oneYearInSeconds,
  });
}
