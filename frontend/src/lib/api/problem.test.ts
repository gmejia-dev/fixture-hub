import { describe, expect, it } from "vitest";
import { toProblem } from "./problem";

describe("toProblem", () => {
  it("lee el código, la metadata y el traceId de un ProblemDetails", () => {
    const body = {
      title: "Conflict",
      status: 409,
      detail: "Ya existe un equipo con el nombre Halcones.",
      errorCode: "Team.NameTaken",
      metadata: { name: "Halcones" },
      traceId: "4bf92f3577b34da6a3ce929d0e0e4736",
    };

    expect(toProblem(409, body, "otro")).toEqual({
      status: 409,
      errorCode: "Team.NameTaken",
      metadata: { name: "Halcones" },
      fieldErrors: undefined,
      traceId: "4bf92f3577b34da6a3ce929d0e0e4736",
    });
  });

  it("separa los errores por campo de Validation.Failed", () => {
    const body = {
      errorCode: "Validation.Failed",
      metadata: {
        errors: { name: ["Team.NameRequired"], "goals[0].minute": ["Goal.InvalidMinute"] },
      },
    };

    expect(toProblem(400, body, null).fieldErrors).toEqual({
      name: ["Team.NameRequired"],
      "goals[0].minute": ["Goal.InvalidMinute"],
    });
  });

  it("usa el header X-Trace-Id si el cuerpo no es un ProblemDetails", () => {
    expect(toProblem(502, undefined, "trace-del-header")).toEqual({
      status: 502,
      errorCode: "",
      metadata: {},
      fieldErrors: undefined,
      traceId: "trace-del-header",
    });
  });
});
