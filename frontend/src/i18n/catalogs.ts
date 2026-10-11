import type { Locale } from "./config";
import en from "./messages/en.json";
import es from "./messages/es.json";

export type MessageKey = keyof typeof es;

export type Messages = Record<MessageKey, string>;

export const catalogs: Record<Locale, Messages> = { es, en };
