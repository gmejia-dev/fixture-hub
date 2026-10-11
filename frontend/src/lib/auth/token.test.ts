import { describe, expect, it } from "vitest";
import { readTokenClaims } from "./token";

const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString("base64url");

const token = (payload: object) => `${encode({ alg: "HS256" })}.${encode(payload)}.firma`;

describe("readTokenClaims", () => {
  it("lee el usuario, el rol y el vencimiento del payload", () => {
    expect(readTokenClaims(token({ name: "admin", role: "Admin", exp: 1_790_000_000 }))).toEqual({
      username: "admin",
      role: "Admin",
      expiresAt: new Date(1_790_000_000_000),
    });
  });

  it.each([
    ["sin payload", "solo-una-parte"],
    ["payload que no es JSON", "a.no-es-json.c"],
    ["sin vencimiento", token({ name: "admin", role: "Admin" })],
  ])("devuelve null con un token %s", (_, value) => {
    expect(readTokenClaims(value)).toBeNull();
  });
});
