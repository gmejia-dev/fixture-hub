import type { Problem } from "@/lib/api/problem";

export type ActionState = {
  status: "idle" | "success" | "error";
  problem?: Problem;
  idempotencyKey?: string;
  values?: Record<string, string>;
};

export const idleState: ActionState = { status: "idle" };
