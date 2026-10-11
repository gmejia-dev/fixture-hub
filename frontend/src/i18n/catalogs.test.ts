import { describe, expect, it } from "vitest";
import en from "./messages/en.json";
import es from "./messages/es.json";

const placeholders = (text: string) => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1]).sort();

describe("catálogos", () => {
  it("tienen exactamente las mismas claves en español y en inglés", () => {
    expect(Object.keys(en).sort()).toEqual(Object.keys(es).sort());
  });

  it("no tienen textos vacíos", () => {
    const empty = [...Object.entries(es), ...Object.entries(en)].filter(([, text]) => !text.trim());

    expect(empty).toEqual([]);
  });

  it("usan los mismos marcadores en los dos idiomas", () => {
    const english = en as Record<string, string>;
    const different = Object.entries(es).filter(
      ([key, text]) => placeholders(text).join() !== placeholders(english[key] ?? "").join(),
    );

    expect(different).toEqual([]);
  });

  it("traducen los códigos que genera el propio frontend", () => {
    expect(es).toHaveProperty(["error.unknown"]);
    expect(es).toHaveProperty(["error.Network.Unavailable"]);
  });
});
