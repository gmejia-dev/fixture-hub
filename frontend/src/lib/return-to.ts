export function safeReturnTo(value: string | null | undefined): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) {
    return "/";
  }

  return value;
}

export function pathFromUrl(url: string | null, origin: string | null): string {
  if (!url) return "/";

  try {
    const parsed = new URL(url);
    if (origin !== null && parsed.host !== origin) return "/";

    return safeReturnTo(parsed.pathname + parsed.search);
  } catch {
    return "/";
  }
}
