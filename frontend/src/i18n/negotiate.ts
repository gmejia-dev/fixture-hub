import { defaultLocale, isLocale, type Locale } from "./config";

export function negotiateLocale(chosen: string | undefined, acceptLanguage: string | null): Locale {
  if (isLocale(chosen)) return chosen;

  for (const tag of preferredLanguages(acceptLanguage)) {
    const language = tag.split("-")[0].toLowerCase();
    if (isLocale(language)) return language;
  }

  return defaultLocale;
}

export function preferredLanguages(acceptLanguage: string | null): string[] {
  if (!acceptLanguage) return [];

  return acceptLanguage
    .split(",")
    .map((part, index) => {
      const [tag, ...parameters] = part.trim().split(";");
      const quality = parameters.map((p) => p.trim()).find((p) => p.startsWith("q="));
      return { tag: tag.trim(), quality: quality ? Number(quality.slice(2)) : 1, index };
    })
    .filter(({ tag, quality }) => tag !== "" && tag !== "*" && quality > 0)
    .sort((a, b) => b.quality - a.quality || a.index - b.index)
    .map(({ tag }) => tag);
}
