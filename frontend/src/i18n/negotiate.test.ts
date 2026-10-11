import { describe, expect, it } from "vitest";
import { negotiateLocale, preferredLanguages } from "./negotiate";

describe("negotiateLocale", () => {
  it("usa primero el idioma que eligió la persona", () => {
    expect(negotiateLocale("en", "es-SV,es;q=0.9")).toBe("en");
  });

  it("sin elección, usa el idioma del navegador", () => {
    expect(negotiateLocale(undefined, "en-US,en;q=0.9,es;q=0.8")).toBe("en");
  });

  it("respeta las preferencias del navegador por calidad", () => {
    expect(negotiateLocale(undefined, "fr;q=0.9,en;q=0.5,es;q=0.8")).toBe("es");
  });

  it("ignora una cookie con un idioma que no existe", () => {
    expect(negotiateLocale("de", "en")).toBe("en");
  });

  it("si nada coincide, usa español", () => {
    expect(negotiateLocale(undefined, "fr-FR,de;q=0.5")).toBe("es");
    expect(negotiateLocale(undefined, null)).toBe("es");
  });
});

describe("preferredLanguages", () => {
  it("ordena por calidad, conserva el orden en empates y descarta q=0 y el comodín", () => {
    expect(preferredLanguages("es-SV, en;q=0.8, fr;q=0, *;q=0.1, pt;q=0.8")).toEqual([
      "es-SV",
      "en",
      "pt",
    ]);
  });
});
