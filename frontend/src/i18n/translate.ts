import type { MessageKey, Messages } from "./catalogs";

export type MessageValues = Record<string, string | number>;

export type Translator = {
  (key: MessageKey, values?: MessageValues): string;
  has(key: string): key is MessageKey;
};

const placeholder = /\{(\w+)\}/g;

export function interpolate(template: string, values: MessageValues = {}): string {
  return template.replace(placeholder, (match, name: string) =>
    Object.hasOwn(values, name) ? String(values[name]) : match,
  );
}

export function createTranslator(messages: Messages): Translator {
  const t = (key: MessageKey, values?: MessageValues) => interpolate(messages[key], values);
  const has = (key: string): key is MessageKey => Object.hasOwn(messages, key);

  return Object.assign(t, { has });
}
