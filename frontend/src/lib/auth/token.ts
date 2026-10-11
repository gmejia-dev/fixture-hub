export type TokenClaims = {
  username: string;
  role: string;
  expiresAt: Date;
};

export function readTokenClaims(token: string): TokenClaims | null {
  const payload = token.split(".")[1];
  if (!payload) return null;

  try {
    const claims: unknown = JSON.parse(Buffer.from(payload, "base64url").toString("utf8"));
    if (typeof claims !== "object" || claims === null) return null;

    const { name, role, exp } = claims as Record<string, unknown>;
    if (typeof name !== "string" || typeof role !== "string" || typeof exp !== "number")
      return null;

    return { username: name, role, expiresAt: new Date(exp * 1000) };
  } catch {
    return null;
  }
}
