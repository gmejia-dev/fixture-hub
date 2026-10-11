import "server-only";
import { cookies } from "next/headers";
import type { AuthToken } from "@/lib/api/types";
import { readTokenClaims } from "./token";

export type Session = {
  username: string;
  isAdmin: boolean;
  expiresAt: Date;
};

const sessionCookieName = "fixturehub_session";

const adminRole = "Admin";

export async function getSession(): Promise<Session | null> {
  const token = await getAccessToken();
  const claims = token ? readTokenClaims(token) : null;
  if (!claims) return null;

  return {
    username: claims.username,
    isAdmin: claims.role === adminRole,
    expiresAt: claims.expiresAt,
  };
}

export async function getAccessToken(): Promise<string | null> {
  const token = (await cookies()).get(sessionCookieName)?.value;
  if (!token) return null;

  const claims = readTokenClaims(token);
  return claims && claims.expiresAt.getTime() > Date.now() ? token : null;
}

export async function startSession(token: AuthToken): Promise<void> {
  (await cookies()).set(sessionCookieName, token.accessToken, {
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    expires: new Date(token.expiresAt),
  });
}

export async function endSession(): Promise<void> {
  (await cookies()).delete(sessionCookieName);
}
