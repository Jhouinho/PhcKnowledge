import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "./apiError";
import {
  ANTIFORGERY_HEADER,
  ANTIFORGERY_PATH,
  apiRequest,
  resetAntiforgeryCache
} from "./httpClient";

describe("httpClient antiforgery", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    resetAntiforgeryCache();
  });

  it("sends X-CSRF-TOKEN on POST when antiforgery is required", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url === ANTIFORGERY_PATH) {
        return new Response(JSON.stringify({ token: "csrf-token-1" }), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      }

      expect(init?.method).toBe("POST");
      const headers = new Headers(init?.headers);
      expect(headers.get(ANTIFORGERY_HEADER)).toBe("csrf-token-1");
      expect(init?.credentials).toBe("same-origin");

      return new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    });

    vi.stubGlobal("fetch", fetchMock);

    const result = await apiRequest<{ ok: boolean }>("/api/portal/example", {
      method: "POST",
      requireAntiforgery: true,
      body: { decision: "confirmed" }
    });

    expect(result.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(String(fetchMock.mock.calls[0]![0])).toBe(ANTIFORGERY_PATH);
  });

  it("maps HTTP 409 to ApiError conflict", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        new Response(JSON.stringify({ error: "A análise não é elegível para esta decisão." }), {
          status: 409,
          headers: { "Content-Type": "application/json" }
        })
      )
    );

    await expect(apiRequest("/api/portal/example")).rejects.toMatchObject({
      status: 409,
      code: "conflict"
    } satisfies Partial<ApiError>);
  });
});
