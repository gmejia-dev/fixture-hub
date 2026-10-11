import { describe, expect, it } from "vitest";
import { isGuid } from "./ids";

describe("isGuid", () => {
  it("acepta un Guid con o sin mayúsculas", () => {
    expect(isGuid("01a12b90-3b7b-7ce3-ba2f-5b5b79dac3d6")).toBe(true);
    expect(isGuid("01A12B90-3B7B-7CE3-BA2F-5B5B79DAC3D6")).toBe(true);
  });

  it.each(["", "abc", "../teams", "01a12b90-3b7b-7ce3-ba2f-5b5b79dac3d6/annul", 42, null])(
    "rechaza lo que no es un Guid (%s)",
    (value) => {
      expect(isGuid(value)).toBe(false);
    },
  );
});
