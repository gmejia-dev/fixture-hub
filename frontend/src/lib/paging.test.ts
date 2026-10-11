import { describe, expect, it } from "vitest";
import { activeSort, hrefWith, listQuery, nextSort, parseListParams } from "./paging";

const sortFields = ["name", "country"];

describe("parseListParams", () => {
  it("lee página, tamaño y orden válidos", () => {
    expect(
      parseListParams(
        { pageNumber: "3", pageSize: "25", sortBy: "country", sortDirection: "DESC" },
        sortFields,
      ),
    ).toEqual({ pageNumber: 3, pageSize: 25, sortBy: "country", sortDirection: "desc" });
  });

  it("descarta valores inválidos y usa los de por defecto", () => {
    expect(
      parseListParams(
        { pageNumber: "-1", pageSize: "1000", sortBy: "Name; DROP TABLE", sortDirection: "up" },
        sortFields,
      ),
    ).toEqual({ pageNumber: 1, pageSize: 10, sortBy: undefined, sortDirection: undefined });
  });

  it("toma el primer valor si el parámetro viene repetido", () => {
    expect(parseListParams({ pageNumber: ["2", "5"] }, sortFields).pageNumber).toBe(2);
  });
});

describe("hrefWith", () => {
  it("combina los parámetros actuales con los cambios y omite los vacíos", () => {
    expect(
      hrefWith(
        "/teams",
        { name: "de", pageNumber: 3, country: undefined },
        { pageNumber: undefined },
      ),
    ).toBe("/teams?name=de");
  });

  it("codifica los valores", () => {
    expect(hrefWith("/teams", { name: "Águilas & Co" })).toBe("/teams?name=%C3%81guilas+%26+Co");
  });

  it("devuelve la ruta sola si no hay parámetros", () => {
    expect(hrefWith("/teams", {})).toBe("/teams");
  });
});

describe("listQuery", () => {
  it("omite la página y el tamaño por defecto para que la URL quede limpia", () => {
    expect(listQuery({ pageNumber: 1, pageSize: 10, sortBy: "name" })).toEqual({
      pageNumber: undefined,
      pageSize: undefined,
      sortBy: "name",
      sortDirection: undefined,
    });
  });
});

describe("orden", () => {
  it("invierte la dirección al volver a elegir la columna activa", () => {
    const list = { pageNumber: 1, pageSize: 10, sortBy: "name", sortDirection: "asc" as const };

    expect(nextSort(list, "name", "name", "asc")).toEqual({
      sortBy: "name",
      sortDirection: "desc",
    });
  });

  it("empieza en la dirección propia de la columna al cambiar de columna", () => {
    const list = { pageNumber: 1, pageSize: 10 };

    expect(nextSort(list, "points", "position", "desc")).toEqual({
      sortBy: "points",
      sortDirection: "desc",
    });
  });

  it("marca como activa la columna por defecto si la URL no trae orden", () => {
    const list = { pageNumber: 1, pageSize: 10 };

    expect(activeSort(list, "position", "position")).toBe("asc");
    expect(activeSort(list, "points", "position")).toBeUndefined();
  });
});
