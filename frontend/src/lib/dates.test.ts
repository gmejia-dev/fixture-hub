import { describe, expect, it } from "vitest";
import {
  dateTimeInputToIso,
  endOfDayIso,
  formatDateTime,
  isDateInput,
  startOfDayIso,
  toDateTimeInput,
} from "./dates";

const elSalvador = "America/El_Salvador";
const newYork = "America/New_York";

describe("dateTimeInputToIso", () => {
  it("convierte la hora local del torneo a UTC", () => {
    expect(dateTimeInputToIso("2026-11-07T19:00", elSalvador)).toBe("2026-11-08T01:00:00.000Z");
  });

  it("usa el desfase vigente en esa fecha cuando la zona tiene horario de verano", () => {
    expect(dateTimeInputToIso("2026-07-01T12:00", newYork)).toBe("2026-07-01T16:00:00.000Z");
    expect(dateTimeInputToIso("2026-12-01T12:00", newYork)).toBe("2026-12-01T17:00:00.000Z");
  });

  it("corrige el desfase cuando la hora cae justo después de un cambio de horario", () => {
    expect(dateTimeInputToIso("2026-03-08T03:30", newYork)).toBe("2026-03-08T07:30:00.000Z");
  });

  it("rechaza textos con otro formato o fechas que no existen", () => {
    expect(dateTimeInputToIso("", elSalvador)).toBeNull();
    expect(dateTimeInputToIso("2026-11-07 19:00", elSalvador)).toBeNull();
    expect(dateTimeInputToIso("2026-02-30T10:00", elSalvador)).toBeNull();
  });
});

describe("toDateTimeInput", () => {
  it("muestra un instante como hora local del torneo", () => {
    expect(toDateTimeInput("2026-11-08T01:00:00Z", elSalvador)).toBe("2026-11-07T19:00");
  });

  it("es la inversa de dateTimeInputToIso", () => {
    const iso = dateTimeInputToIso("2026-10-11T08:05", elSalvador)!;

    expect(toDateTimeInput(iso, elSalvador)).toBe("2026-10-11T08:05");
  });
});

describe("días completos", () => {
  it("cubren desde las 00:00 hasta las 23:59:59.999 locales", () => {
    expect(startOfDayIso("2026-10-11", elSalvador)).toBe("2026-10-11T06:00:00.000Z");
    expect(endOfDayIso("2026-10-11", elSalvador)).toBe("2026-10-12T05:59:59.999Z");
  });

  it("solo aceptan fechas reales en formato AAAA-MM-DD", () => {
    expect(isDateInput("2026-10-11")).toBe(true);
    expect(isDateInput("2026-13-01")).toBe(false);
    expect(isDateInput("11/10/2026")).toBe(false);
    expect(isDateInput(undefined)).toBe(false);
  });
});

describe("formatDateTime", () => {
  it("formatea en el idioma y la zona indicados", () => {
    const text = formatDateTime("2026-11-08T01:00:00Z", "en", elSalvador);

    expect(text).toContain("Nov 7, 2026");
    expect(text).toContain("7:00");
  });
});
