import "server-only";

export function newIdempotencyKey(): string {
  return crypto.randomUUID();
}
