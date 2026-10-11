import { describe, expect, it } from "vitest";
import { catalogs } from "./catalogs";
import { createTranslator, interpolate } from "./translate";

describe("interpolate", () => {
  it("reemplaza cada marcador con su valor", () => {
    expect(
      interpolate("El dorsal {shirtNumber} de {team}", { shirtNumber: 10, team: "Halcones" }),
    ).toBe("El dorsal 10 de Halcones");
  });

  it("deja el marcador si falta el valor", () => {
    expect(interpolate("Hola {name}")).toBe("Hola {name}");
  });

  it("no toma valores heredados del prototipo", () => {
    expect(interpolate("{toString}", {})).toBe("{toString}");
  });
});

describe("createTranslator", () => {
  it("traduce según el catálogo del idioma", () => {
    expect(createTranslator(catalogs.es)("status.InProgress")).toBe("En curso");
    expect(createTranslator(catalogs.en)("status.InProgress")).toBe("In progress");
  });

  it("sabe si una clave existe", () => {
    const t = createTranslator(catalogs.es);

    expect(t.has("error.Team.NameTaken")).toBe(true);
    expect(t.has("error.Team.NoExiste")).toBe(false);
    expect(t.has("constructor")).toBe(false);
  });
});
