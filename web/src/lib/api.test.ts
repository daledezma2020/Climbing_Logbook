import { beforeEach, describe, expect, it, vi } from "vitest";
import { API_BASE, apiFetch } from "@/lib/api";

describe("apiFetch", () => {
  beforeEach(() => vi.stubGlobal("fetch", vi.fn()));

  it("sends auth and JSON headers and returns JSON", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ id: 12 }), { status: 201 }));
    const result = await apiFetch<{ id: number }>("/climbs/manual", {
      method: "POST",
      accessToken: "token-123",
      body: JSON.stringify({ name: "Test" }),
    });

    expect(result).toEqual({ id: 12 });
    const headers = new Headers(vi.mocked(fetch).mock.calls[0]![1]?.headers);
    expect(headers.get("Authorization")).toBe("Bearer token-123");
    expect(headers.get("Content-Type")).toBe("application/json");
  });

  it("returns undefined for a successful empty response", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 204 }));
    await expect(apiFetch<void>("/climbs/1", { method: "DELETE" })).resolves.toBeUndefined();
  });

  it("surfaces validation details with the endpoint", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({
      title: "Validation failed",
      errors: { Name: ["Name is required."] },
    }), { status: 400 }));

    await expect(apiFetch("/logentries")).rejects.toThrow(
      "Validation failed Name: Name is required. Endpoint: /logentries.",
    );
  });

  it("distinguishes a network failure from an API response", async () => {
    vi.mocked(fetch).mockRejectedValue(new TypeError("connection refused"));
    await expect(apiFetch("/places")).rejects.toThrow(
      `Could not reach the API at ${API_BASE}. connection refused`,
    );
  });
});
