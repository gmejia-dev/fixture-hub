import { describe, expect, it } from "vitest";
import { pathFromUrl, safeReturnTo } from "./return-to";

describe("safeReturnTo", () => {
  it("acepta rutas internas con su query string", () => {
    expect(safeReturnTo("/matches/1?status=InProgress")).toBe("/matches/1?status=InProgress");
  });

  it.each(["https://evil.example", "//evil.example", "/\\evil.example", "matches", "", null])(
    "manda al inicio si el destino no es una ruta interna (%s)",
    (value) => {
      expect(safeReturnTo(value)).toBe("/");
    },
  );
});

describe("pathFromUrl", () => {
  it("extrae la ruta de una URL del mismo sitio", () => {
    expect(pathFromUrl("http://localhost:3000/teams/1?name=a", "localhost:3000")).toBe(
      "/teams/1?name=a",
    );
  });

  it("ignora URLs de otro sitio o inválidas", () => {
    expect(pathFromUrl("http://evil.example/teams", "localhost:3000")).toBe("/");
    expect(pathFromUrl("no es una url", "localhost:3000")).toBe("/");
    expect(pathFromUrl(null, "localhost:3000")).toBe("/");
  });
});
