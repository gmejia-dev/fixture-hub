import { describe, expect, it } from "vitest";
import { networkProblem, toProblem } from "@/lib/api/problem";
import { catalogs } from "./catalogs";
import { describeProblem } from "./problems";
import { createTranslator } from "./translate";

const es = createTranslator(catalogs.es);
const en = createTranslator(catalogs.en);
const formatDateTime = (iso: string) => `fecha(${iso})`;

describe("describeProblem", () => {
  it("arma el mensaje con el código y los valores de metadata", () => {
    const problem = toProblem(
      409,
      { errorCode: "Player.ShirtNumberTaken", metadata: { shirtNumber: 10 }, traceId: "abc" },
      null,
    );

    expect(describeProblem(es, problem, formatDateTime)).toEqual({
      message: "El dorsal 10 ya pertenece a otro jugador del equipo.",
      fields: {},
      traceId: "abc",
    });
    expect(describeProblem(en, problem, formatDateTime).message).toBe(
      "Shirt number 10 already belongs to another player on the team.",
    );
  });

  it("traduce los estados del partido que vienen en metadata", () => {
    const problem = toProblem(
      409,
      {
        errorCode: "Match.InvalidStatusTransition",
        metadata: { currentStatus: "Finished", targetStatus: "InProgress" },
      },
      null,
    );

    expect(describeProblem(es, problem, formatDateTime).message).toBe(
      "Un partido en estado «Finalizado» no puede pasar a «En curso».",
    );
  });

  it("formatea las fechas que vienen en metadata", () => {
    const problem = toProblem(
      409,
      { errorCode: "Match.ScheduleConflict", metadata: { scheduledAt: "2026-11-08T01:00:00Z" } },
      null,
    );

    expect(describeProblem(es, problem, formatDateTime).message).toBe(
      "Uno de los equipos ya tiene un partido el fecha(2026-11-08T01:00:00Z).",
    );
  });

  it("traduce cada error de validación por campo", () => {
    const problem = toProblem(
      400,
      {
        errorCode: "Validation.Failed",
        metadata: { errors: { name: ["Team.NameRequired"], country: ["Team.CountryTooLong"] } },
      },
      null,
    );

    expect(describeProblem(es, problem, formatDateTime)).toMatchObject({
      message: "Revisa los datos marcados.",
      fields: {
        name: ["El nombre del equipo es obligatorio."],
        country: ["El país del equipo no puede superar 60 caracteres."],
      },
    });
  });

  it("muestra un mensaje genérico, nunca la clave, si el código no tiene traducción", () => {
    const problem = toProblem(500, { errorCode: "Algo.Nuevo" }, "trace-1");

    expect(describeProblem(es, problem, formatDateTime)).toMatchObject({
      message: "Ocurrió un error inesperado. Intenta de nuevo.",
      traceId: "trace-1",
    });
  });

  it("explica cuando la Api no respondió", () => {
    expect(describeProblem(es, networkProblem(), formatDateTime).message).toBe(
      "No se pudo conectar con el servidor. Intenta de nuevo en un momento.",
    );
  });
});
