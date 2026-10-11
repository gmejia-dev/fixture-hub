import { describe, expect, it } from "vitest";
import { GET } from "./route";

describe("GET /health", () => {
  it("responde Healthy sin consultar la Api y sin caché", async () => {
    const response = GET();

    expect(response.status).toBe(200);
    expect(await response.text()).toBe("Healthy");
    expect(response.headers.get("Cache-Control")).toBe("no-store");
  });
});
